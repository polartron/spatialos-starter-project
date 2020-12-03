using System;

namespace StarterProject.Shared.Time
{
    public class TimeUtils
    {
        public static long CurrentTimeInMs()
        {
#if UNITY_EDITOR
            //We want to be able to pause the editor
            return (long) (UnityEngine.Time.time * 1000f);
#else
            return (long) (DateTime.Now - TimeConfig.TimeStart).TotalMilliseconds;
#endif

        }
    }
}
