using System;
using System.Diagnostics;
using System.Threading.Tasks;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using Caliburn.Micro;
using NWaveform.Events;

namespace NWaveform.ViewModels
{
    public class WaveformDisplayViewModel : Screen, IWaveformDisplayViewModel
    {
        private double _duration;
        private WriteableBitmap _waveformImage;
        private int[] _leftChannel;
        private int[] _rightChannel;
        private int _width;
        protected int ZeroMagnitude { get; private set; }

        private SolidColorBrush _backgroundBrush = new SolidColorBrush(WaveformSettings.DefaultBackgroundColor);
        private SolidColorBrush _leftBrush = new SolidColorBrush(WaveformSettings.DefaultLeftColor);
        private SolidColorBrush _rightBrush = new SolidColorBrush(WaveformSettings.DefaultRightColor);
        private Uri _source;

        public WaveformDisplayViewModel(IEventAggregator events)
        {
            WaveformImage = BitmapFactory.New(1920, 1080);
            events.Subscribe(this);
        }

        internal WriteableBitmap WaveformImage
        {
            get { return _waveformImage; }
            set
            {
                if (value == null) throw new ArgumentNullException();
                _waveformImage = value;
                ZeroMagnitude = (int)(_waveformImage.Height / 2.0);
                _width = (int)_waveformImage.Width;
                if (_leftChannel == null) _leftChannel = new int[_width]; else Array.Resize(ref _leftChannel, _width);
                if (_rightChannel == null) _rightChannel = new int[_width]; else Array.Resize(ref _rightChannel, _width);
                _leftChannel.Set(ZeroMagnitude);
                _rightChannel.Set(ZeroMagnitude);
                _waveformImage.Clear(BackgroundBrush.Color);
            }
        }

        public Uri Source
        {
            get { return _source; }
            set
            {
                if (Equals(value, _source)) return;
                _source = value;
                NotifyOfPropertyChange();
            }
        }

        public double Duration
        {
            get { return _duration; }
            set
            {
                if (Math.Abs(_duration - value) < double.Epsilon) return;
                _duration = value;
                NotifyOfPropertyChange();
                NotifyOfPropertyChange(nameof(WaveformViewModel.HasDuration));
            }
        }

        public SolidColorBrush LeftBrush
        {
            get { return _leftBrush; }
            set { _leftBrush = value; NotifyOfPropertyChange(); RenderWaveform(); }
        }

        public SolidColorBrush RightBrush
        {
            get { return _rightBrush; }
            set { _rightBrush = value; NotifyOfPropertyChange(); RenderWaveform(); }
        }

        public int[] LeftChannel => _leftChannel;
        public int[] RightChannel => _rightChannel;

        public SolidColorBrush BackgroundBrush
        {
            get { return _backgroundBrush; }
            set { _backgroundBrush = value; NotifyOfPropertyChange(); RenderWaveform(); }
        }

        protected override void OnViewLoaded(object view)
        {
            var myView = view as IHaveWaveformImage;
            if (myView != null)
                myView.WaveformImageBrush.ImageSource = WaveformImage;
        }

        protected override void OnActivate()
        {
            RenderWaveform();
        }

        public Task Handle(PeaksReceivedEvent message)
        {
            if (!SameSource(message)) return Task.FromResult(0);
            return Execute.OnUIThreadAsync(() => HandlePeaks(message));
        }

        internal void HandlePeaks(PeaksReceivedEvent message)
        {
            var pointsReceivedEvent = message.ToPoints(Duration, WaveformImage.Width, WaveformImage.Height);
            Handle(pointsReceivedEvent);
            Trace.WriteLine($"Received #{message.Peaks.Length} peaks ({message.Start}:{message.End}) for '{message.Source}' ");
        }

        private void Handle(PointsReceivedEvent message)
        {
            _leftChannel.FlushedCopy(message.XOffset, message.LeftPoints, ZeroMagnitude);
            _rightChannel.FlushedCopy(message.XOffset, message.RightPoints, ZeroMagnitude);
            RenderWaveform(message.XOffset);
        }

        private bool SameSource(PeaksReceivedEvent message)
        {
            return Source != null && message.Source == Source;
        }

        protected void RenderWaveform(int x0 = 0, int len = 0)
        {
            var w = (int)WaveformImage.Width;
            var h = (int)WaveformImage.Height;
            var h2 = h / 2;

            // clear background from x0 to x1
            var x1 = len > 0 ? x0 + len : w;
            var backColor = WriteableBitmapExtensions.ConvertColor(BackgroundBrush.Color);
            WaveformImage.FillRectangle(x0, 0, x1, h, backColor);

            using (var ctx = WaveformImage.GetBitmapContext())
            {
                var leftColor = WriteableBitmapExtensions.ConvertColor(LeftBrush.Color);
                var rightColor = WriteableBitmapExtensions.ConvertColor(RightBrush.Color);
                for (var x = x0; x < _leftChannel.Length; x++)
                {
                    WriteableBitmapExtensions.DrawLine(ctx, w, h, x, h2, x, _leftChannel[x], leftColor);
                    WriteableBitmapExtensions.DrawLine(ctx, w, h, x, h2, x, _rightChannel[x], rightColor);
                }
            }
        }
    }
}