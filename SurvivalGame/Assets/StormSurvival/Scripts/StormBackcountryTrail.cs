using System;
using UnityEngine;

namespace StormSurvival
{
    // The marked tracks are visual. The terrain remains the walking surface across bends and grades.
    internal static class StormBackcountryTrail
    {
        private static readonly Vector2 RangerShed = new Vector2(-16f, 282f);
        private static readonly Vector2 WestBreaker = new Vector2(-19f, 351.5f);
        private static readonly Vector2 EastBreaker = new Vector2(20f, 388f);
        private static readonly Vector2 Relay = new Vector2(-4f, 407.5f);

        private static readonly (Vector2[] Points, float Width, bool Spur)[] Routes =
        {
            (new[] { new Vector2(0f, 249f), new Vector2(0f, 264f), new Vector2(-.9f, 275f),
                new Vector2(1.1f, 288f), new Vector2(0f, 303.1f) }, 4.55f, false),
            (new[] { new Vector2(-.9f, 275f), new Vector2(-7f, 277.5f), new Vector2(-13f, 280f),
                RangerShed, new Vector2(-12.35f, 283.9f), new Vector2(-12.4f, 289.65f), new Vector2(-10.7f, 294f),
                new Vector2(-5.8f, 297.7f), new Vector2(0f, 303.1f) }, 3.35f, true),
            (new[] { new Vector2(0f, 321.8f), new Vector2(0f, 330f), new Vector2(-2.2f, 339f),
                new Vector2(1.4f, 352f), new Vector2(-.2f, 364f), new Vector2(1.9f, 376f),
                new Vector2(-1.8f, 390f), new Vector2(.6f, 403f), new Vector2(.6f, 410f) }, 4.25f, false),
            (new[] { new Vector2(-2.2f, 339f), new Vector2(-9.5f, 344.4f), new Vector2(-16.8f, 348.6f),
                WestBreaker, new Vector2(-18f, 358.8f), new Vector2(-10f, 364.8f), new Vector2(.8f, 369f) }, 3.45f, true),
            (new[] { new Vector2(.8f, 369f), new Vector2(8.8f, 375.1f), new Vector2(15.2f, 380.5f),
                EastBreaker, new Vector2(16f, 396.5f), new Vector2(8.2f, 401.7f), new Vector2(.6f, 405f) }, 3.45f, true),
            (new[] { new Vector2(.6f, 403f), Relay, new Vector2(-1.6f, 411.9f) }, 3.7f, true),
            (new[] { new Vector2(.6f, 410f), new Vector2(-3.6f, 414.1f), new Vector2(-10.6f, 423.5f),
                new Vector2(-8.3f, 433.5f), new Vector2(-2.6f, 439.4f), new Vector2(0f, 444f),
                new Vector2(0f, 446f) }, 4.45f, false),
        };

        internal static bool NearTrail(float x, float z)
        {
            var position = new Vector2(x, z);
            foreach (var (points, width, _) in Routes)
            {
                float corridor = width * .5f + 1.6f;
                for (int index = 0; index < points.Length - 1; index++)
                {
                    Vector2 segment = points[index + 1] - points[index];
                    float t = Mathf.Clamp01(Vector2.Dot(position - points[index], segment) / segment.sqrMagnitude);
                    if ((position - (points[index] + segment * t)).sqrMagnitude < corridor * corridor) return true;
                }
            }

            return (position - RangerShed).sqrMagnitude < 32f || (position - WestBreaker).sqrMagnitude < 20f ||
                (position - EastBreaker).sqrMagnitude < 20f || (position - Relay).sqrMagnitude < 32f ||
                (position - new Vector2(5f, 267f)).sqrMagnitude < 38f ||
                (position - new Vector2(-6f, 331f)).sqrMagnitude < 38f ||
                (position - new Vector2(4f, 441f)).sqrMagnitude < 86f;
        }

        internal static void Build(StormWorld world, Transform parent, Action<Bounds> registerShelter)
        {
            var route = Group("Backcountry trails", parent, Vector3.zero);
            var mud = world.MakeMaterial(new Color(.35f, .29f, .20f));
            var spur = world.MakeMaterial(new Color(.24f, .21f, .16f));
            var puddle = world.MakeMaterial(new Color(.15f, .20f, .20f));
            puddle.SetFloat("_Smoothness", .79f);
            var slate = world.MakeMaterial(new Color(.25f, .31f, .29f));
            var canopy = world.MakeMaterial(new Color(.24f, .31f, .27f));
            var teal = world.MakeMaterial(new Color(.23f, .82f, .73f), true);
            var random = new System.Random(27341);

            foreach (var (points, width, isSpur) in Routes)
            {
                var path = Group(isSpur ? "Ranger track" : "Muddy summit trail", route, Vector3.zero);
                for (int index = 0; index < points.Length - 1; index++)
                    LayTrack(world, path, points[index], points[index + 1], width, isSpur ? spur : mud, puddle, random);
                MarkTrail(world, path, points, width, isSpur ? teal : world.Amber);
            }

            DirectionPost(world, route, Ground(new Vector2(-2.8f, 274f)), -1, teal);
            DirectionPost(world, route, Ground(new Vector2(-4.2f, 337.8f)), -1, teal);
            DirectionPost(world, route, Ground(new Vector2(3.2f, 368f)), 1, teal);
            DirectionPost(world, route, Ground(new Vector2(-1.65f, 402.8f)), -1, teal);
            BuildRangerShed(world, route, slate, canopy, teal, registerShelter);
            BuildUtilitySite(world, route, WestBreaker, "West breaker clearing", slate, teal);
            BuildUtilitySite(world, route, EastBreaker, "East breaker clearing", slate, teal);
            BuildRelayShelter(world, route, slate, canopy, teal, registerShelter);
            BuildWashoutScene(world, route, slate);
        }

        private static void LayTrack(StormWorld world, Transform parent, Vector2 start, Vector2 end,
            float width, Material soil, Material puddle, System.Random random)
        {
            float distance = Vector2.Distance(start, end);
            int pieces = Mathf.Max(1, Mathf.CeilToInt(distance / 4.8f));
            for (int index = 0; index < pieces; index++)
            {
                Vector3 a = Ground(Vector2.Lerp(start, end, (float)index / pieces));
                Vector3 b = Ground(Vector2.Lerp(start, end, (float)(index + 1) / pieces));
                Vector3 mid = (a + b) * .5f;
                float rise = StormWorld.Height(mid.z + .35f) - StormWorld.Height(mid.z - .35f);
                Quaternion frame = Quaternion.LookRotation(b - a, new Vector3(0f, .7f, -rise).normalized);
                float edgeVariation = (float)random.NextDouble() * .32f;
                Flat(world, parent, "Worn track bed", mid + Vector3.up * .052f, frame,
                    new Vector3(width + edgeVariation, .035f, Vector3.Distance(a, b) + .62f), soil);

                if (random.Next(3) != 0) continue;
                float side = random.Next(2) == 0 ? -.8f : .8f;
                var sheen = world.Shape("Rain pooled in the track", PrimitiveType.Sphere, parent,
                    mid + frame * (Vector3.right * side) + Vector3.up * .076f,
                    new Vector3(.48f + (float)random.NextDouble() * .72f, .035f, .9f + (float)random.NextDouble()), puddle, false);
                sheen.transform.localRotation = frame;
            }
        }

        private static void MarkTrail(StormWorld world, Transform parent, Vector2[] points, float width, Material marker)
        {
            float next = 7f;
            for (int index = 0; index < points.Length - 1; index++)
            {
                Vector2 segment = points[index + 1] - points[index];
                float distance = segment.magnitude;
                var right = new Vector2(segment.y, -segment.x).normalized;
                while (next < distance)
                {
                    Vector2 pose = points[index] + segment * (next / distance) + right * (width * .5f + .42f);
                    // The bridge winch and ranger equipment each have their own bright markings.
                    if (pose.y < 298.5f || pose.y > 325f)
                        Reflector(world, parent, Ground(pose), marker, .92f);
                    next += 12.5f;
                }
                next -= distance;
            }
        }

        private static void Reflector(StormWorld world, Transform parent, Vector3 foot, Material marker, float height)
        {
            world.Shape("Trail reflector stake", PrimitiveType.Cube, parent, foot + Vector3.up * (height * .5f),
                new Vector3(.12f, height, .12f), world.Wood, false);
            world.Shape("Reflective route strip", PrimitiveType.Cube, parent, foot + Vector3.up * (height - .10f),
                new Vector3(.22f, .12f, .15f), marker, false);
        }

        private static void DirectionPost(StormWorld world, Transform parent, Vector3 foot, int side, Material marker)
        {
            Reflector(world, parent, foot, marker, 1.45f);
            Vector3 face = foot + new Vector3(side * .09f, 1.2f, -.09f);
            world.Shape("Timber junction sign", PrimitiveType.Cube, parent, face + Vector3.forward * .045f,
                new Vector3(.86f, .38f, .10f), world.Wood, false);
            Flat(world, parent, "Reflective trail arrow", face + Vector3.left * (side * .10f), Quaternion.identity,
                new Vector3(.47f, .052f, .035f), marker);
            for (int arm = -1; arm <= 1; arm += 2)
                Flat(world, parent, "Reflective trail arrowhead", face + new Vector3(side * .20f, arm * .055f, 0f),
                    Quaternion.Euler(0f, 0f, -side * arm * 42f), new Vector3(.19f, .052f, .035f), marker);
        }

        private static void BuildRangerShed(StormWorld world, Transform parent, Material slate, Material roof,
            Material marker, Action<Bounds> registerShelter)
        {
            Vector3 center = Ground(RangerShed);
            var shed = Group("Storm-damaged ranger shed", parent, center);
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
                world.Shape("Ranger shed timber post", PrimitiveType.Cube, shed,
                    new Vector3(side * 3.06f, 1.35f, end * 2.48f), new Vector3(.24f, 2.72f, .24f), world.Wood);

            for (int index = 0; index < 13; index++)
            {
                float x = (index - 6) * .47f;
                world.Shape("Water-stained wall board", PrimitiveType.Cube, shed, new Vector3(x, 1.29f, 2.48f),
                    new Vector3(.454f, 2.62f, .18f), world.Wood);
            }
            for (int index = 0; index < 9; index++)
            {
                float z = (index - 4) * .54f;
                float height = index < 3 ? 1.09f + .19f * index : 2.58f;
                world.Shape("Splintered siding", PrimitiveType.Cube, shed, new Vector3(-3.06f, height * .5f, z),
                    new Vector3(.18f, height, .52f), world.Wood);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                var roofPanel = world.Shape("Weathered ranger shed roof", PrimitiveType.Cube, shed,
                    new Vector3(side * 1.73f, 2.95f, .07f), new Vector3(3.67f, .20f, 5.77f), roof);
                roofPanel.transform.localRotation = Quaternion.Euler(0f, 0f, side * -10f);
            }
            Flat(world, shed, "Worn stone shed floor", new Vector3(0f, .057f, 0f), Quaternion.identity,
                new Vector3(5.96f, .05f, 4.86f), slate);
            Reflector(world, shed, new Vector3(-2.56f, 0f, -2.63f), marker, 1.45f);
            registerShelter(new Bounds(center + Vector3.up * 1.25f, new Vector3(5.38f, 2.6f, 4.3f)));
        }

        private static void BuildUtilitySite(StormWorld world, Transform parent, Vector2 center, string name,
            Material slate, Material marker)
        {
            var site = Group(name, parent, Ground(center));
            float slope = (StormWorld.Height(center.y + 1f) - StormWorld.Height(center.y - 1f)) * .5f;
            var angle = Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0f, 0f);
            Flat(world, site, "Utility station gravel pad", Vector3.up * .08f, angle, new Vector3(4.5f, .052f, 4.3f), slate);
            for (int side = -1; side <= 1; side += 2)
            {
                var foot = Ground(center + new Vector2(side * 2.16f, 1.86f)) - site.position;
                world.Shape("Weathered utility marker", PrimitiveType.Cylinder, site, foot + Vector3.up * .47f,
                    new Vector3(.22f, .48f, .22f), world.Wood);
                world.Shape("Utility marker cap", PrimitiveType.Cube, site, foot + Vector3.up * .86f,
                    new Vector3(.25f, .13f, .25f), marker, false);
            }
        }

        private static void BuildRelayShelter(StormWorld world, Transform parent, Material slate, Material roof,
            Material marker, Action<Bounds> registerShelter)
        {
            var shelter = Group("High ridge relay shelter", parent, Ground(Relay));
            var slope = (StormWorld.Height(Relay.y + 1f) - StormWorld.Height(Relay.y - 1f)) * .5f;
            var angle = Quaternion.Euler(-Mathf.Atan(slope) * Mathf.Rad2Deg, 0f, 0f);
            Flat(world, shelter, "Relay station gravel pad", Vector3.up * .08f, angle, new Vector3(4.65f, .052f, 4.85f), slate);
            for (int side = -1; side <= 1; side += 2)
            for (int end = -1; end <= 1; end += 2)
            {
                Vector3 foot = Ground(Relay + new Vector2(side * 2.47f, end * 2.53f)) - shelter.position;
                world.Shape("Relay shelter timber post", PrimitiveType.Cube, shelter, foot + Vector3.up * 1.39f,
                    new Vector3(.21f, 2.83f, .22f), world.Wood);
            }
            var awning = world.Shape("Relay shelter roof", PrimitiveType.Cube, shelter, Vector3.up * 2.83f,
                new Vector3(5.45f, .18f, 5.68f), roof);
            awning.transform.localRotation = angle;
            Reflector(world, shelter, new Vector3(-1.94f, -2.44f * slope, -2.44f), marker, 1.25f);
            registerShelter(new Bounds(shelter.position + Vector3.up * 1.29f, new Vector3(4.49f, 2.7f, 4.57f)));
        }

        private static void BuildWashoutScene(StormWorld world, Transform parent, Material slate)
        {
            var scenery = Group("Eroded washout banks", parent, Vector3.zero);
            Reflector(world, scenery, Ground(new Vector2(2.48f, 301.3f)), world.Amber, 1.42f);
            Reflector(world, scenery, Ground(new Vector2(2.48f, 322.3f)), world.Amber, 1.42f);
            var random = new System.Random(82742);
            for (int side = -1; side <= 1; side += 2)
            for (int index = 0; index < 6; index++)
            {
                float x = side * (5.65f + index * 1.52f);
                float near = 302.8f + (float)random.NextDouble() * 2.4f;
                float far = 319.6f + (float)random.NextDouble() * 2.9f;
                float size = .48f + (float)random.NextDouble() * .42f;
                foreach (float z in new[] { near, far })
                {
                    Vector3 foot = Ground(new Vector2(x, z));
                    var boulder = world.Shape("Eroded ledge stone", PrimitiveType.Sphere, scenery, foot + Vector3.up * .10f,
                        new Vector3(size * 1.8f, size * .76f, size * 1.46f), slate, false);
                    boulder.transform.localRotation = Quaternion.Euler(16f, (float)random.NextDouble() * 280f, -14f);
                }
            }
        }

        private static void Flat(StormWorld world, Transform parent, string title, Vector3 center, Quaternion frame,
            Vector3 size, Material material)
        {
            var piece = world.Shape(title, PrimitiveType.Cube, parent, center, size, material, false);
            piece.transform.localRotation = frame;
        }

        private static Transform Group(string title, Transform parent, Vector3 position)
        {
            var child = new GameObject(title).transform;
            child.SetParent(parent, false);
            child.localPosition = position;
            return child;
        }

        private static Vector3 Ground(Vector2 position)
        {
            return new Vector3(position.x, StormWorld.Height(position.y), position.y);
        }
    }
}
