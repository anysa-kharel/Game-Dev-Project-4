using System;
using UnityEngine;

namespace StormSurvival
{
    internal static class StormRidgeRoute
    {
        private const float ForkZ = 180f;
        private const float RoofHeight = 3.15f;
        private const int RestBayIndex = 2;

        private static readonly Vector3 RidgeNormal = new Vector3(0f, 1f, -.22f).normalized;
        private static readonly Vector2[] CoveredTrail =
        {
            new Vector2(0f, ForkZ),
            new Vector2(-3.4f, 183f),
            new Vector2(-10f, 188f),
            new Vector2(-10f, 198f),
            new Vector2(-5f, 207f),
            new Vector2(-4.45f, 219f),
            new Vector2(-10f, 226f),
            new Vector2(-7.4f, 232f),
            new Vector2(-3.3f, 235f),
            new Vector2(0f, 239f),
        };

        public static bool ApproachingFork(Vector3 position, Vector3 forward)
        {
            return Mathf.Abs(position.x) < 2.8f && position.z >= ForkZ - 5f &&
                position.z <= ForkZ + 5f && forward.z > .3f;
        }

        public static void Build(StormWorld world, Transform parent, Action<Bounds> registerShelter,
            Action<Vector3, Vector3> placeSupplies)
        {
            var fork = new GameObject("Ridge route fork").transform;
            fork.SetParent(parent, false);
            var roofMaterial = world.MakeMaterial(new Color(.17f, .25f, .23f));
            var boardMaterial = world.MakeMaterial(new Color(.10f, .14f, .13f));
            var pathMaterial = world.MakeMaterial(new Color(.24f, .19f, .12f));
            var coverMarker = world.MakeMaterial(new Color(.24f, .86f, .76f), true);

            Wayfinder(world, fork, Ground(new Vector2(-2.55f, 180.9f)), Vector2.left, boardMaterial, coverMarker);
            Wayfinder(world, fork, Ground(new Vector2(2.35f, 181.3f)), Vector2.up, boardMaterial, world.Amber);

            for (int index = 0; index < CoveredTrail.Length - 1; index++)
            {
                Vector3 start = Ground(CoveredTrail[index]);
                Vector3 end = Ground(CoveredTrail[index + 1]);
                bool restBay = index == RestBayIndex;
                float width = restBay ? 5.2f : 4.1f;
                float overlapOffset = (index % 2) * .025f;
                Track(world, fork, start, end, width - .55f, pathMaterial, overlapOffset);

                // The approach and exit transition between the exposed trail and canopy.
                if (index == 0 || index == CoveredTrail.Length - 2) continue;
                RoofedBay(world, fork, start, end, width, roofMaterial, coverMarker, overlapOffset, registerShelter);
            }

            // Leave the center clear so players can pass the supplies without detouring.
            placeSupplies(Ground(new Vector2(-11.6f, 194f)), Ground(new Vector2(-11.6f, 196.2f)));
            Wayfinder(world, fork, Ground(new Vector2(-2.55f, 237.7f)), Vector2.up, boardMaterial, world.Amber);
        }

        private static void Track(StormWorld world, Transform parent, Vector3 start, Vector3 end,
            float width, Material material, float overlapOffset)
        {
            var rotation = Quaternion.LookRotation(end - start, RidgeNormal);
            float length = Vector3.Distance(start, end);
            var track = world.Shape("Covered ridge footpath", PrimitiveType.Cube, parent,
                (start + end) * .5f + Vector3.up * (.029f + overlapOffset), new Vector3(width, .035f, length + .22f), material, false);
            track.transform.localRotation = rotation;

            int slats = Mathf.FloorToInt(length / 2.3f);
            for (int index = 0; index < slats; index++)
            {
                Vector3 center = Vector3.Lerp(start, end, (index + .5f) / slats);
                var slat = world.Shape("Weathered trail sleeper", PrimitiveType.Cube, parent,
                    center + Vector3.up * (.06f + overlapOffset), new Vector3(Mathf.Min(2.7f, width - .35f), .038f, .19f),
                    world.Wood, false);
                slat.transform.localRotation = rotation;
            }
        }

        private static void RoofedBay(StormWorld world, Transform parent, Vector3 start, Vector3 end,
            float width, Material roofMaterial, Material marker, float overlapOffset, Action<Bounds> registerShelter)
        {
            Vector3 forward = (end - start).normalized;
            Quaternion rotation = Quaternion.LookRotation(forward, RidgeNormal);
            Vector3 side = rotation * Vector3.right;
            float length = Vector3.Distance(start, end);
            var roof = world.Shape("Covered ridge roof", PrimitiveType.Cube, parent,
                (start + end) * .5f + Vector3.up * (RoofHeight + overlapOffset),
                new Vector3(width, .19f, length + .65f), roofMaterial);
            roof.transform.localRotation = rotation;

            int frames = Mathf.Max(2, Mathf.CeilToInt(length / 6f) + 1);
            for (int index = 0; index < frames; index++)
            {
                float fraction = Mathf.Lerp(.07f, .93f, index / (frames - 1f));
                Vector3 center = Vector3.Lerp(start, end, fraction);
                var frame = world.Shape("Canopy crossbeam", PrimitiveType.Cube, parent,
                    center + Vector3.up * (2.96f + overlapOffset), new Vector3(width, .22f, .24f), world.Wood, false);
                frame.transform.localRotation = rotation;

                for (int sign = -1; sign <= 1; sign += 2)
                {
                    Vector3 foot = center + side * (sign * (width * .5f - .16f));
                    world.Shape("Canopy post", PrimitiveType.Cube, parent, foot + Vector3.up * 1.5f,
                        new Vector3(.18f, 3.05f, .18f), world.Wood);

                    if (index != 0) continue;
                    world.Shape("Covered route reflector", PrimitiveType.Cube, parent, foot + Vector3.up * 1.8f,
                        new Vector3(.24f, .18f, .24f), marker, false);
                }
            }

            // The shelter system accepts axis-aligned volumes. Small overlapping samples
            // hug the angled roof without giving the exposed shortcut accidental cover.
            int intervals = Mathf.CeilToInt(length / 1.15f);
            float coverWidth = (width - .45f) / Mathf.Sqrt(2f);
            for (int index = 0; index <= intervals; index++)
            {
                Vector3 center = Vector3.Lerp(start, end, index / (float)intervals);
                registerShelter(new Bounds(center + Vector3.up * 1.17f, new Vector3(coverWidth, 2.5f, coverWidth)));
            }
        }

        private static void Wayfinder(StormWorld world, Transform parent, Vector3 position,
            Vector2 direction, Material boardMaterial, Material marker)
        {
            world.Shape("Ridge wayfinder post", PrimitiveType.Cube, parent,
                position + Vector3.up * .83f, new Vector3(.14f, 1.66f, .14f), world.Wood);
            Vector3 center = position + Vector3.up * 1.6f;
            world.Shape("Ridge directional sign", PrimitiveType.Cube, parent, center,
                new Vector3(.72f, .72f, .11f), boardMaterial, false);

            Vector2 perpendicular = new Vector2(-direction.y, direction.x);
            Vector2 tip = direction * .2f;
            ArrowStroke(world, parent, center, -direction * .2f, tip, marker);
            ArrowStroke(world, parent, center, tip, direction * .022f + perpendicular * .17f, marker);
            ArrowStroke(world, parent, center, tip, direction * .022f - perpendicular * .17f, marker);
        }

        private static void ArrowStroke(StormWorld world, Transform parent, Vector3 center, Vector2 start, Vector2 end,
            Material material)
        {
            Vector2 stroke = end - start;
            Vector2 midpoint = (start + end) * .5f;
            var marking = world.Shape("Ridge wayfinder arrow", PrimitiveType.Cube, parent,
                center + new Vector3(midpoint.x, midpoint.y, -.069f), new Vector3(stroke.magnitude, .053f, .015f),
                material, false);
            marking.transform.localRotation = Quaternion.Euler(0f, 0f, Mathf.Atan2(stroke.y, stroke.x) * Mathf.Rad2Deg);
        }

        private static Vector3 Ground(Vector2 point)
        {
            return new Vector3(point.x, StormWorld.Height(point.y), point.y);
        }
    }
}
