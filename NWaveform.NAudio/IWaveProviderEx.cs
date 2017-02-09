﻿using System;
using NAudio.Wave;

namespace NWaveform.NAudio
{
    public interface IWaveProviderEx : IWaveProvider
    {
        TimeSpan CurrentTime { get; set; }
        TimeSpan TotalTime { get; }
        float Volume { get; set; }
        float Pan { get; set; }
        bool SupportsPanning { get; }
    }
}