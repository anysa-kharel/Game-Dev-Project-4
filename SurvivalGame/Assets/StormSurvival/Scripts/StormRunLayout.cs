using UnityEngine;

namespace StormSurvival
{
    public sealed class StormRunLayout
    {
        public const int PreviewSeed = 824;

        // The sled pulls toward negative X. Each rack stays on the right shoulder
        // with room to slide onto open ground beside the trail.
        private static readonly Vector2[] SledSites =
        {
            new Vector2(5.6f, 72.75f),
            new Vector2(5.1f, 79.5f),
            new Vector2(5.3f, 96.25f),
        };

        private static readonly (Vector2 Food, Vector2 Water)[] FoodStashes =
        {
            // Both props sit off the walking lane, with a clear approach from the trail.
            (new Vector2(5.2f, 65f), new Vector2(5.2f, 68.5f)),
            (new Vector2(5.4f, 104.5f), new Vector2(5.4f, 107.2f)),
            (new Vector2(-5.4f, 112.75f), new Vector2(-5.4f, 115.3f)),
        };

        private static readonly Vector2[] SpareFuelSites =
        {
            new Vector2(5.4f, 157.5f),
            new Vector2(-5.2f, 160.5f),
            new Vector2(5.4f, 174f),
        };

        public StormRunLayout(int seed)
        {
            Seed = seed;
            var random = new System.Random(seed);

            SledSite = Ground(SledSites[random.Next(SledSites.Length)]);
            var (food, water) = FoodStashes[random.Next(FoodStashes.Length)];
            StashFoodSite = Ground(food);
            StashWaterSite = Ground(water);
            StashMarkerSite = MarkerTowardTrail((food + water) * .5f);

            Vector2 spareFuel = SpareFuelSites[random.Next(SpareFuelSites.Length)];
            SpareFuelSite = Ground(spareFuel);
            SpareFuelMarkerSite = MarkerTowardTrail(spareFuel);

            WindSeed = random.Next();
            WolfSeed = random.Next();
        }

        public int Seed { get; }
        public Vector3 SledSite { get; }
        public Vector3 StashFoodSite { get; }
        public Vector3 StashWaterSite { get; }
        public Vector3 StashMarkerSite { get; }
        public Vector3 SpareFuelSite { get; }
        public Vector3 SpareFuelMarkerSite { get; }
        public int WindSeed { get; }
        public int WolfSeed { get; }

        private static Vector3 MarkerTowardTrail(Vector2 site)
        {
            site.x -= Mathf.Sign(site.x) * 1.45f;
            return Ground(site);
        }

        private static Vector3 Ground(Vector2 site)
        {
            return new Vector3(site.x, StormWorld.Height(site.y), site.y);
        }
    }
}
