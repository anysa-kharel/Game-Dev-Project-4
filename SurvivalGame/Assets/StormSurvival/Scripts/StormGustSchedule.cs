using System;

namespace StormSurvival
{
    public sealed class StormGustSchedule
    {
        private readonly Random random;
        private readonly float lullMin, lullMax, gustMin, gustMax, force;
        private float remaining;

        public bool Gusting { get; private set; }
        public float UntilGust => Gusting ? 0f : remaining;
        public float Strength => Gusting ? force : 0f;
        public int Direction { get; private set; }

        public StormGustSchedule(int seed)
        {
            random = new Random(seed);
            switch (random.Next(3))
            {
                case 0:
                    // Wide windows to cross without a gust.
                    (lullMin, lullMax, gustMin, gustMax, force) = (13f, 19f, 2.8f, 3.8f, .85f);
                    break;
                case 1:
                    // Brief and frequent bursts.
                    (lullMin, lullMax, gustMin, gustMax, force) = (5.8f, 8.5f, 2.4f, 3.3f, .95f);
                    break;
                default:
                    // Longer, stronger crosswinds separated by a moderate lull.
                    (lullMin, lullMax, gustMin, gustMax, force) = (8.5f, 13f, 3.8f, 5f, 1.15f);
                    break;
            }
            Direction = random.Next(2) == 0 ? -1 : 1;
            remaining = Range(lullMin, lullMax);
        }

        public void Advance(float seconds)
        {
            if (!(seconds > 0f) || float.IsInfinity(seconds)) return;
            while (seconds >= remaining)
            {
                seconds -= remaining;
                Gusting = !Gusting;
                if (Gusting) remaining = Range(gustMin, gustMax);
                else
                {
                    remaining = Range(lullMin, lullMax);
                    if (random.Next(3) == 0) Direction = -Direction;
                }
            }
            remaining -= seconds;
        }

        private float Range(float min, float max) => min + (max - min) * (float)random.NextDouble();
    }
}
