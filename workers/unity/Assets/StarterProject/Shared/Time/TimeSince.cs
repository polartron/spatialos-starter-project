namespace StarterProject.Shared.Time
{
    public struct TimeSince
    {
        private float _time;

        public static implicit operator float(TimeSince ts)
        {
            return UnityEngine.Time.time - ts._time;
        }

        public static implicit operator TimeSince(float ts)
        {
            return new TimeSince { _time = UnityEngine.Time.time - ts };
        }
    }
}
