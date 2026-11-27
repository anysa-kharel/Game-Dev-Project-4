using UnityEngine;
using UnityEngine.Rendering;

namespace StormSurvival
{
    public sealed class StormSearchlight : MonoBehaviour
    {
        private static readonly string[] Bearings = { "West ridge", "North ridge", "East ridge" };
        private const int RescueBearing = 2;
        private const float TurnSpeed = 40f;
        private const float ReplyDuration = 3.8f;

        private StormGame game;
        private Generator generator;
        private Transform head;
        private Transform beam;
        private Light spot;
        private LineRenderer beamCore;
        private Renderer lens;
        private Material unlitLens;
        private Material poweredLens;
        private GameObject replyLamp;
        private Mesh beamMesh;
        private Vector3[] targets;
        private int bearing;
        private bool lit;
        private float alignedAt;

        public bool Powered => generator != null && generator.Toggled && generator.CurrentFuel > 0f;
        public bool Aligned { get; private set; }

        public string Prompt
        {
            get
            {
                if (!Powered) return "Start the generator to power the spotlight";
                if (Aligned) return "Signal received  /  Light the rescue beacon";
                return $"E  /  Turn spotlight  ·  {Bearings[bearing]}";
            }
        }

        public static StormSearchlight Build(StormGame game, StormWorld world, Transform lookout,
            Generator generator, GameObject spotlightModel)
        {
            if (spotlightModel == null)
            {
                spotlightModel = new GameObject("Rescue spotlight");
                spotlightModel.transform.SetParent(lookout, false);
                spotlightModel.transform.localPosition = new Vector3(-4f, -.44f, -2f);
                world.Shape("Spotlight stand", PrimitiveType.Cylinder, spotlightModel.transform,
                    new Vector3(0f, .8f, 0f), new Vector3(.46f, .8f, .46f), world.Metal);
            }

            spotlightModel.name = "Rescue spotlight";
            var station = spotlightModel.AddComponent<StormSearchlight>();
            station.game = game;
            station.generator = generator;
            station.PrepareLamp(world, spotlightModel);
            station.BuildDistantClearing(world, lookout);
            station.head.rotation = station.DesiredRotation();
            if (Application.isPlaying) station.BuildBeam(lookout, game.particleMaterial);
            return station;
        }

        private void PrepareLamp(StormWorld world, GameObject model)
        {
            if (model.TryGetComponent<Rigidbody>(out var body))
            {
                body.isKinematic = true;
                body.useGravity = false;
            }

            var legacy = model.GetComponentInChildren<Spotlight>(true);
            if (legacy != null)
            {
                legacy.enabled = false;
                if (legacy.Lights != null) legacy.Lights.SetActive(false);
                lens = legacy.lightCube;
                unlitLens = legacy.LampOff;
                poweredLens = legacy.LampOn;
                if (lens != null && unlitLens != null) lens.sharedMaterial = unlitLens;
            }

            head = new GameObject("Rotating spotlight head").transform;
            head.SetParent(model.transform, false);
            var lamp = model.transform.Find("SpotLamp");
            if (lamp != null)
            {
                float localY = lens != null ? model.transform.InverseTransformPoint(lens.bounds.center).y : .344f;
                head.localPosition = new Vector3(0f, localY, 0f);
                lamp.SetParent(head, true);
                if (lamp.TryGetComponent<MeshFilter>(out var housing) && housing.sharedMesh != null)
                {
                    var bounds = housing.sharedMesh.bounds;
                    var hitBox = lamp.gameObject.AddComponent<BoxCollider>();
                    hitBox.center = bounds.center;
                    hitBox.size = bounds.size;
                }
                var handle = model.transform.Find("SpotHandle");
                if (handle != null) handle.SetParent(head, true);
                return;
            }

            head.localPosition = new Vector3(0f, 1.6f, 0f);
            world.Shape("Lamp housing", PrimitiveType.Cube, head, Vector3.zero,
                new Vector3(.72f, .52f, .48f), world.Metal);
            unlitLens = world.MakeMaterial(new Color(.26f, .31f, .30f));
            poweredLens = world.MakeMaterial(new Color(.92f, .92f, .75f), true);
            lens = world.Shape("Lamp lens", PrimitiveType.Cube, head, new Vector3(0f, 0f, .25f),
                new Vector3(.58f, .40f, .06f), unlitLens, false).GetComponent<Renderer>();
        }

        private void BuildDistantClearing(StormWorld world, Transform lookout)
        {
            // Keep the signal atop the opposite ridge, above the playable forest's perimeter rocks.
            var ridgeStone = world.MakeMaterial(new Color(.16f, .22f, .22f));
            var landingGround = world.MakeMaterial(new Color(.26f, .32f, .24f));
            world.Shape("West ridge silhouette", PrimitiveType.Sphere, lookout, new Vector3(-32f, 4f, 47f),
                new Vector3(46f, 23f, 36f), ridgeStone, false);
            world.Shape("North ridge silhouette", PrimitiveType.Sphere, lookout, new Vector3(-5f, 4f, 57f),
                new Vector3(39f, 22f, 41f), ridgeStone, false);
            world.Shape("East ridge silhouette", PrimitiveType.Sphere, lookout, new Vector3(26f, 4f, 54f),
                new Vector3(42f, 24f, 36f), ridgeStone, false);

            var clearing = new GameObject("Rescue clearing").transform;
            clearing.SetParent(lookout, false);
            clearing.localPosition = new Vector3(24f, 15.5f, 53f);
            world.Shape("Landing ground", PrimitiveType.Cylinder, clearing, new Vector3(0f, -.18f, 0f),
                new Vector3(12f, .36f, 12f), landingGround, false);

            // A triangle of amber lamps distinguishes the clearing at a distance.
            for (int i = -1; i <= 1; i++)
            {
                float height = i == 0 ? 3.2f : 1.65f;
                float x = i * 3.15f;
                world.Shape("Signal mast", PrimitiveType.Cube, clearing, new Vector3(x, height * .5f, 0f),
                    new Vector3(.22f, height, .22f), world.Metal, false);
                world.Shape("Amber signal lamp", PrimitiveType.Sphere, clearing, new Vector3(x, height, 0f),
                    new Vector3(1.35f, 1.35f, .92f), world.Amber, false);
            }

            var replyMaterial = world.MakeMaterial(new Color(.86f, .98f, 1f), true);
            replyLamp = world.Shape("Answering rescue flash", PrimitiveType.Sphere, clearing, new Vector3(0f, 4.45f, 0f),
                new Vector3(2.25f, 2.25f, 1.35f), replyMaterial, false);
            var replyLight = replyLamp.AddComponent<Light>();
            replyLight.type = LightType.Point;
            replyLight.color = new Color(.78f, .93f, 1f);
            replyLight.range = 15f;
            replyLight.intensity = 3.5f;
            replyLamp.SetActive(false);

            targets = new[]
            {
                lookout.TransformPoint(new Vector3(-30f, 15.5f, 43f)),
                lookout.TransformPoint(new Vector3(-4f, 16.7f, 56f)),
                clearing.TransformPoint(new Vector3(0f, 2.5f, 0f))
            };
        }

        private void BuildBeam(Transform lookout, Material particles)
        {
            beam = new GameObject("Searchlight beam").transform;
            beam.SetParent(lookout, false);
            beam.gameObject.SetActive(false);
            spot = beam.gameObject.AddComponent<Light>();
            spot.type = LightType.Spot;
            spot.spotAngle = 11f;
            spot.color = new Color(.79f, .89f, 1f);
            spot.intensity = 6f;
            spot.shadows = LightShadows.None;

            if (particles == null) return;
            beamMesh = MakeBeamMesh();
            beam.gameObject.AddComponent<MeshFilter>().sharedMesh = beamMesh;
            var renderer = beam.gameObject.AddComponent<MeshRenderer>();
            renderer.sharedMaterial = particles;
            renderer.shadowCastingMode = ShadowCastingMode.Off;
            renderer.receiveShadows = false;

            var core = new GameObject("Searchlight glow");
            core.transform.SetParent(beam, false);
            beamCore = core.AddComponent<LineRenderer>();
            beamCore.sharedMaterial = particles;
            beamCore.useWorldSpace = true;
            beamCore.alignment = LineAlignment.View;
            beamCore.positionCount = 2;
            beamCore.startWidth = .22f;
            beamCore.endWidth = 3.1f;
            beamCore.startColor = new Color(.86f, .94f, 1f, .77f);
            beamCore.endColor = new Color(.80f, .91f, 1f, .43f);
            beamCore.shadowCastingMode = ShadowCastingMode.Off;
            beamCore.receiveShadows = false;
        }

        private static Mesh MakeBeamMesh()
        {
            // Overlapping soft ribbons give the cone volume without opaque geometry.
            const int ribbons = 5;
            const int rows = 5;
            const int columns = 3;
            var distances = new[] { 0f, .055f, .20f, .70f, 1f };
            var opacities = new[] { 0f, .42f, .30f, .21f, .075f };
            var vertices = new Vector3[ribbons * rows * columns];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[ribbons * (rows - 1) * (columns - 1) * 6];
            int triangle = 0;
            for (int ribbon = 0; ribbon < ribbons; ribbon++)
            {
                float angle = ribbon * Mathf.PI / ribbons;
                var direction = new Vector3(Mathf.Cos(angle), Mathf.Sin(angle), 0f);
                for (int row = 0; row < rows; row++)
                {
                    float distance = distances[row];
                    for (int column = 0; column < columns; column++)
                    {
                        int index = (ribbon * rows + row) * columns + column;
                        float spread = (column - 1) * Mathf.Lerp(.18f, 3.8f, distance);
                        vertices[index] = direction * spread + Vector3.forward * distance;
                        colors[index] = new Color(.82f, .91f, 1f, opacities[row]);
                        uv[index] = new Vector2(column * .5f, .5f);
                        if (row == rows - 1 || column == columns - 1) continue;
                        triangles[triangle++] = index;
                        triangles[triangle++] = index + columns;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + 1;
                        triangles[triangle++] = index + columns;
                        triangles[triangle++] = index + columns + 1;
                    }
                }
            }

            var mesh = new Mesh { name = "Searchlight volume", vertices = vertices, uv = uv, colors = colors, triangles = triangles };
            mesh.RecalculateBounds();
            return mesh;
        }

        public void Rotate()
        {
            if (!Powered)
            {
                game.Toast("Start the generator beside the lookout first.");
                return;
            }
            if (Aligned) return;

            bearing = (bearing + 1) % Bearings.Length;
            game.Audio.Play("spotlight turn", transform.position, .65f);
        }

        private Quaternion DesiredRotation()
        {
            return Quaternion.LookRotation(targets[bearing] - head.position, Vector3.up);
        }

        private void Update()
        {
            if (beam == null) return;
            bool powered = Powered;
            if (lit != powered)
            {
                lit = powered;
                beam.gameObject.SetActive(powered);
                if (lens != null)
                {
                    var material = powered ? poweredLens : unlitLens;
                    if (material != null) lens.sharedMaterial = material;
                }
            }

            if (!powered)
            {
                if (replyLamp.activeSelf) replyLamp.SetActive(false);
                return;
            }

            var desired = DesiredRotation();
            head.rotation = Quaternion.RotateTowards(head.rotation, desired, TurnSpeed * Time.deltaTime);
            var origin = lens != null ? lens.bounds.center + head.forward * .10f : head.position + head.forward * .28f;
            float length = Mathf.Max(1f, Vector3.Distance(origin, targets[bearing]));
            beam.SetPositionAndRotation(origin, head.rotation);
            beam.localScale = new Vector3(1f, 1f, length);
            spot.range = length + 3f;
            if (beamCore != null)
            {
                beamCore.SetPosition(0, origin);
                beamCore.SetPosition(1, origin + head.forward * length);
            }

            if (!Aligned && bearing == RescueBearing && Quaternion.Angle(head.rotation, desired) <= .5f)
            {
                Aligned = true;
                alignedAt = Time.time;
                game.Audio.Play("signal reply", transform.position, .82f);
                game.Toast("The clearing flashed back. Light the rescue beacon.", 8f);
            }

            UpdateReply();
        }

        private void UpdateReply()
        {
            if (!Aligned) return;
            float elapsed = Time.time - alignedAt;
            bool flash = elapsed < ReplyDuration && (elapsed % 1.05f < .33f);
            if (replyLamp.activeSelf != flash) replyLamp.SetActive(flash);
        }

        private void OnDestroy()
        {
            if (beamMesh == null) return;
            if (Application.isPlaying) Destroy(beamMesh);
            else DestroyImmediate(beamMesh);
        }
    }
}
