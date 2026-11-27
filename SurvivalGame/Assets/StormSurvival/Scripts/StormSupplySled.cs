using UnityEngine;
using UnityEngine.AI;

namespace StormSurvival
{
    public sealed class StormSupplySled : MonoBehaviour
    {
        private const float TravelToFree = 1.35f;
        private const float GrabRange = 4.6f;
        private const float WrapFuel = 60f;
        private const float TorchCapacity = 240f;

        private StormGame game;
        private Rigidbody body;
        private GameObject cargo;
        private Vector3 parkedPosition;
        private float travel;
        private float pullOrigin;
        private float travelAtGrab;
        private bool pulling;
        private bool collected;

        public bool CanTarget => !collected;
        public bool Freed => travel >= TravelToFree;
        public string Prompt => Freed ? "E / take dry supplies" : "Hold right mouse and move toward the trail";

        public static StormSupplySled Build(StormGame game, StormWorld world, Transform parent, Vector3 site, bool isPreview)
        {
            var shelter = new GameObject("Logging camp supply rack").transform;
            shelter.SetParent(parent, false);
            shelter.position = site;

            BuildRack(world, shelter);

            var frame = new GameObject("Jammed supply sled");
            frame.transform.SetParent(shelter, false);
            var bundle = BuildSled(world, frame.transform);
            var collider = frame.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, .34f, 0f);
            collider.size = new Vector3(1.82f, .52f, 1.04f);

            var sled = frame.AddComponent<StormSupplySled>();
            sled.game = game;
            sled.cargo = bundle;
            sled.parkedPosition = site;
            if (!isPreview)
            {
                // A kinematic body keeps the loaded sled on its runners while it is pulled.
                sled.body = frame.AddComponent<Rigidbody>();
                sled.body.useGravity = false;
                sled.body.isKinematic = true;
                var obstacle = frame.AddComponent<NavMeshObstacle>();
                obstacle.shape = NavMeshObstacleShape.Box;
                obstacle.center = collider.center;
                obstacle.size = collider.size;
                obstacle.carving = true;
                obstacle.carveOnlyStationary = false;
            }
            return sled;
        }

        public bool BeginPull(Vector3 playerPosition)
        {
            if (!CanTarget || Freed || !WithinGrabRange(playerPosition)) return false;

            pulling = true;
            pullOrigin = playerPosition.x;
            travelAtGrab = travel;
            return true;
        }

        public bool Pull(Vector3 playerPosition)
        {
            if (!pulling || !WithinGrabRange(playerPosition))
            {
                EndPull();
                return false;
            }

            float distance = Mathf.Clamp(travelAtGrab + pullOrigin - playerPosition.x, travel, TravelToFree);
            if (distance <= travel) return true;

            travel = distance;
            Vector3 position = parkedPosition + Vector3.left * travel;
            if (body != null) body.position = position;
            else transform.position = position;
            if (!Freed) return true;

            EndPull();
            game.Audio.Play("sled release", position, .65f);
            game.Toast("Sled freed. Look at the bundle and press E.", 5f);
            return false;
        }

        public void EndPull()
        {
            pulling = false;
        }

        public void Use()
        {
            if (!Freed || collected || !game.Running) return;

            collected = true;
            cargo.SetActive(false);
            game.AddWood(2);

            var needs = game.Needs;
            if (needs.HasTorch && needs.TorchFuel < TorchCapacity - 1f)
            {
                float fuel = Mathf.Min(WrapFuel, TorchCapacity - needs.TorchFuel);
                needs.TorchFuel += fuel;
                game.Toast("Dry supplies: 2 wood and " + Mathf.RoundToInt(fuel) + " seconds of torch fuel.", 6f);
            }
            else
            {
                needs.Cloth++;
                game.Toast("Dry supplies: 2 wood and 1 cloth.", 6f);
            }
        }

        private bool WithinGrabRange(Vector3 playerPosition)
        {
            Vector3 delta = transform.position - playerPosition;
            delta.y = 0f;
            return delta.sqrMagnitude <= GrabRange * GrabRange;
        }

        private static void BuildRack(StormWorld world, Transform shelter)
        {
            var panel = world.MakeMaterial(new Color(.20f, .25f, .21f));
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * .75f;
                world.Shape("Supply rack post", PrimitiveType.Cube, shelter, new Vector3(.94f, .76f, z),
                    new Vector3(.17f, 1.52f, .17f), world.Wood);
                world.Shape("Sled rail", PrimitiveType.Cube, shelter, new Vector3(-.33f, .04f, side * .45f),
                    new Vector3(2.72f, .08f, .10f), world.Wood, false);
            }
            world.Shape("Rack back brace", PrimitiveType.Cube, shelter, new Vector3(.94f, 1.1f, 0f),
                new Vector3(.16f, .19f, 1.65f), world.Wood);
            var roof = world.Shape("Supply rack rain cover", PrimitiveType.Cube, shelter, new Vector3(.45f, 1.54f, 0f),
                new Vector3(1.45f, .12f, 1.88f), panel);
            roof.transform.localRotation = Quaternion.Euler(0f, 0f, 9f);
            world.Shape("Amber rack marker", PrimitiveType.Cube, shelter, new Vector3(-.25f, 1.51f, 0f),
                new Vector3(.12f, .12f, .46f), world.Amber, false);
        }

        private static GameObject BuildSled(StormWorld world, Transform frame)
        {
            var canvas = world.MakeMaterial(new Color(.67f, .53f, .32f));
            var webbing = world.MakeMaterial(new Color(.26f, .21f, .15f));
            for (int side = -1; side <= 1; side += 2)
            {
                float z = side * .45f;
                world.Shape("Steel sled runner", PrimitiveType.Cube, frame, new Vector3(0f, .11f, z),
                    new Vector3(1.82f, .10f, .12f), world.Metal, false);
                world.Shape("Runner nose", PrimitiveType.Cube, frame, new Vector3(-.83f, .16f, z),
                    new Vector3(.18f, .11f, .12f), world.Metal, false).transform.localRotation = Quaternion.Euler(0f, 0f, -28f);
            }
            world.Shape("Sled deck", PrimitiveType.Cube, frame, new Vector3(0f, .23f, 0f),
                new Vector3(1.65f, .14f, .98f), world.Wood, false);
            world.Shape("Sled pull handle", PrimitiveType.Cube, frame, new Vector3(-.89f, .28f, 0f),
                new Vector3(.12f, .14f, .56f), world.Metal, false);

            var cargo = new GameObject("Dry supply bundle");
            cargo.transform.SetParent(frame, false);
            world.Shape("Canvas supply pack", PrimitiveType.Cube, cargo.transform, new Vector3(.12f, .45f, 0f),
                new Vector3(.99f, .30f, .78f), canvas, false);
            for (int side = -1; side <= 1; side += 2)
            {
                world.Shape("Canvas binding", PrimitiveType.Cube, cargo.transform, new Vector3(.12f + side * .27f, .60f, 0f),
                    new Vector3(.08f, .035f, .80f), webbing, false);
            }
            world.Shape("Amber pull tag", PrimitiveType.Cube, cargo.transform, new Vector3(-.41f, .45f, 0f),
                new Vector3(.05f, .12f, .29f), world.Amber, false);
            return cargo;
        }
    }
}
