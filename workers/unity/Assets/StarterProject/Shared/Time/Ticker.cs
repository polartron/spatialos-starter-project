using System;
using System.Collections.Generic;
using UnityEngine;

namespace StarterProject.Shared.Time
{
    public interface ITickable
    {
        void Tick(float deltaTime, long tick);
    }

    public struct TickerData
    {
        public long LastTick;
        public double TimeUpdated;
        public double Offset;
        public double TargetTime;

        public float DilationFrom;
        public float DilationTo;
    }

    public struct TickerConfig
    {
        public int TicksPerSecond;
        public double SmoothTimeMs;

        public float DilationAmountMs;
        public float DilationLengthMs;
        public AnimationCurve DilationCurve;

        public static TickerConfig Default = new TickerConfig()
        {
            DilationCurve = new AnimationCurve(new[]
            {
                new Keyframe(0, 0), new Keyframe(0.25f, 1f), new Keyframe(0.75f, 1f), new Keyframe(1f, 0f),
            }),
            DilationAmountMs = 50,
            DilationLengthMs = 3000,
            TicksPerSecond = 20,
            SmoothTimeMs = 500
        };
    }

    public class Ticker
    {
        private TickerData _tickerData;
        private readonly TickerConfig _tickerConfig;

        private List<ITickable> _tickables = new List<ITickable>();
        public double TickFloat => GetTickFloat(_tickerData, _tickerConfig);
        public double Dilation => GetDilationAmount(_tickerData, _tickerConfig, TickFloat);

        public Ticker(long timeInMs)
        {
            _tickerConfig = TickerConfig.Default;
            _tickerConfig.TicksPerSecond = TimeConfig.TicksPerSecond;
            _tickerConfig.SmoothTimeMs = TimeConfig.TickerSmoothTimeMs;

            _tickerData = new TickerData()
            {
                TargetTime = timeInMs, Offset = 0, TimeUpdated = TimeUtils.CurrentTimeInMs(), DilationTo = 1
            };
        }

        public void Add(ITickable tickable)
        {
            _tickables.Add(tickable);
        }

        public void Remove(ITickable tickable)
        {
            _tickables.Remove(tickable);
        }

        public void Update()
        {
            var tick = (long) GetTickFloat(_tickerData, _tickerConfig);
            var ticksToSimulate = Mathf.Clamp(tick - _tickerData.LastTick, 0, 100);

            for (int i = 0; i < ticksToSimulate; i++)
            {
                foreach (var fixedUpdate in _tickables)
                {
                    fixedUpdate.Tick(1f / TimeConfig.TicksPerSecond, _tickerData.LastTick + 1 + i);
                }
            }

            _tickerData.LastTick = tick;
        }

        public void Dilate()
        {
            Dilate(ref _tickerData, _tickerConfig, (uint) TickFloat);
        }

        public void SetTime(long time)
        {
            SetTime(ref _tickerData, _tickerConfig, time);
        }

        public static void Dilate(ref TickerData data, in TickerConfig config, uint tick)
        {
            float time = (float) tick / TimeConfig.TicksPerSecond * 1000f;
            float il = Mathf.InverseLerp(data.DilationFrom, data.DilationTo, time);

            if ( il < 0.5f)
            {
                //If we're already increasing dilation then don't do anything
                return;
            }

            // We've finished dilating or dilation amount is decreasing.
            // Flip the inverse lerp so that 1.0 becomes 0.0, 0.9 becomes 0.1, 0.6 becomes 0.4 etc
            // We accomplish this by shifting From and To forwards in time.

            float toLerp = Mathf.RoundToInt(Mathf.Lerp(0, config.DilationLengthMs, il));
            float fromLerp = Mathf.RoundToInt(Mathf.Lerp(0, config.DilationLengthMs, 1.0f - il));

            float from = time - fromLerp;
            float to = time + toLerp;

            data.DilationFrom = from;
            data.DilationTo = to;
        }

        public static void SetTime(ref TickerData data, in TickerConfig config, long time)
        {
            double timeInMs = TimeUtils.CurrentTimeInMs();
            double elapsed = (timeInMs - data.TimeUpdated);
            double current = BaseTime(data, config, timeInMs) + elapsed;

            data.Offset = time - current;
            data.TimeUpdated = timeInMs;
            data.TargetTime = time;
        }

        public static double GetTickFloat(in TickerData data, in TickerConfig config)
        {
            double timeInMs = TimeUtils.CurrentTimeInMs();
            double elapsed = timeInMs - data.TimeUpdated;
            double current = BaseTime(data, config, timeInMs) + elapsed;
            double seconds = current / 1000;

            double tick = seconds * config.TicksPerSecond;
            double dilation = GetDilationAmount(data, config, tick);

            return tick + dilation;
        }

        public static double GetDilationAmount(in TickerData data, in TickerConfig config, double tick)
        {
            double maxTick = config.DilationAmountMs / 1000f * config.TicksPerSecond;
            double time = tick / TimeConfig.TicksPerSecond * 1000f;
            float il = Mathf.InverseLerp(data.DilationFrom, data.DilationTo, (float) time);

            float value = config.DilationCurve.Evaluate(il);
            return value * maxTick;
        }

        private static double BaseTime(in TickerData data, in TickerConfig config, double timeInMs)
        {
            if (timeInMs < data.TimeUpdated + config.SmoothTimeMs)
            {
                double a = data.TimeUpdated;
                double b = data.TimeUpdated + config.SmoothTimeMs;
                double c = timeInMs;

                double inverseLerp = 1.0 - (Math.Abs(a - b) > double.Epsilon
                                         ? Mathf.Clamp01((float) ((c - a) / (b - a)))
                                         : 0.0f);

                return data.TargetTime + data.Offset * inverseLerp;
            }

            return data.TargetTime;
        }
    }
}
