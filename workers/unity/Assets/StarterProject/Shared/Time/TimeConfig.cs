using System;

namespace StarterProject.Shared.Time
{
    public static class TimeConfig
    {
        public static readonly int TicksPerSecond = 20;
        public static readonly int TickerSmoothTimeMs = 500;
        public static readonly int TimeRequestIntervalSeconds = 5;
        public static readonly int InputBufferLengthTicks = 4;
        public static readonly DateTime TimeStart = new DateTime(2020, 1, 1);
        public static readonly long CommandBufferLengthMs = (long) (1000f * (1f / TicksPerSecond) * InputBufferLengthTicks);
    }
}
