using UnityEngine;
using UnityStandardAssets.Characters.FirstPerson;

namespace StormSurvival
{
    // Adapter around the template player; camera, controller, hands and Animator are retained.
    public sealed class StormPlayer : MonoBehaviour
    {
        public Camera View { get; private set; }
        public FirstPersonController Controller { get; private set; }
        public LifeStats Stats { get; private set; }
        public HandInventory Inventory { get; private set; }
        public OilHolder FuelCup { get; private set; }
        public ObjectUsage Usage { get; private set; }
        public bool Crouched { get; private set; }
        public string Prompt => Usage != null ? Usage.CurrentPrompt : "";
        public float HoldProgress => Usage != null ? Usage.HoldProgress : 0f;
        private StormGame game;
        private CharacterController capsule;
        private float originalHeight, originalEye, shake;
        private Vector3 originalCenter;
        private bool exhausted;
        private Transform torchWick;
        private Transform carriedFlame;
        private Mesh flameMesh;
        private MeshRenderer outerFlame, flameCore;
        private ParticleSystemRenderer embers;
        private Light[] torchLights;
        private MaterialPropertyBlock flameTint;
        private static readonly int BaseColor = Shader.PropertyToID("_BaseColor");

        public void Initialize(StormGame owner)
        {
            game = owner; Controller = GetComponent<FirstPersonController>();
            View = GetComponentInChildren<Camera>(true);
            Stats = GetComponent<LifeStats>(); Inventory = GetComponent<HandInventory>();
            FuelCup = GetComponent<OilHolder>(); Usage = GetComponent<ObjectUsage>();
            capsule = GetComponent<CharacterController>();
            originalHeight = capsule.height; originalCenter = capsule.center;
            originalEye = View.transform.localPosition.y;
            capsule.radius = 0.34f; capsule.skinWidth = 0.035f; capsule.stepOffset = 0.25f;
            Stats.ExternallyManaged = true; Stats.Health = 800; Stats.Hunger = 800; Stats.Thirst = 650;
            Controller.ConfigureSpeeds(4.1f, 6.4f); Controller.SetHeadBob(false);
            Controller.Jumped += OnJump;
            Inventory.selected = 2; Inventory.Refresh();
            BuildTorchFlame();
            Usage.PlayerCam = View; Usage.stats = Stats; Usage.inventory = Inventory; Usage.OilHolder = FuelCup;
            FuelCup.HoldingNow = 0;
            foreach (Transform part in GetComponentsInChildren<Transform>(true)) part.gameObject.layer = 2;
            var saved = GetComponent<LocationSaver>(); if (saved != null) saved.enabled = false;
            var debug = GetComponent<RestartPlayerPrefs>(); if (debug != null) debug.DebugMode = false;
            foreach (var source in GetComponentsInChildren<AudioListener>(true)) source.enabled = source.gameObject == View.gameObject;
            View.gameObject.tag = "MainCamera"; View.farClipPlane = 220; View.nearClipPlane = 0.045f;
            View.renderingPath = RenderingPath.UsePlayerSettings;
            Teleport(new Vector3(0, 0.1f, 3));
        }

        private void BuildTorchFlame()
        {
            GameObject torch = Inventory.Slot1;
            torchWick = torch.transform.Find("Torch/Fire");

            // Keep the prefab's wick and light placement, and replace its large flipbook emitters.
            foreach (var system in torch.GetComponentsInChildren<ParticleSystem>(true))
            {
                system.Stop(false, ParticleSystemStopBehavior.StopEmittingAndClear);
                var main = system.main;
                main.playOnAwake = false;
                var renderer = system.GetComponent<ParticleSystemRenderer>();
                renderer.enabled = false;
            }
            torchLights = torch.GetComponentsInChildren<Light>(true);
            foreach (var light in torchLights) light.enabled = false;
            if (torchWick == null || game.particleMaterial == null) return;

            carriedFlame = new GameObject("Carried torch flame").transform;
            carriedFlame.SetParent(transform, false);
            carriedFlame.gameObject.SetActive(false);
            flameTint = new MaterialPropertyBlock();
            flameMesh = CreateFlameMesh();
            outerFlame = AddFlameLayer("Orange flame");
            flameCore = AddFlameLayer("Gold flame core");
            flameCore.transform.localPosition = new Vector3(0, .018f, -.003f);
            flameCore.sortingOrder = 1;

            var sparks = new GameObject("Torch embers");
            sparks.transform.SetParent(carriedFlame, false);
            sparks.transform.localPosition = Vector3.up * .035f;
            var particles = sparks.AddComponent<ParticleSystem>();
            var particleMain = particles.main;
            particleMain.loop = true;
            particleMain.playOnAwake = true;
            particleMain.simulationSpace = ParticleSystemSimulationSpace.Local;
            particleMain.startLifetime = new ParticleSystem.MinMaxCurve(.17f, .35f);
            particleMain.startSpeed = 0f;
            particleMain.startSize = new ParticleSystem.MinMaxCurve(.004f, .009f);
            particleMain.startColor = new Color(1f, .56f, .13f, .95f);
            particleMain.maxParticles = 5;
            var emission = particles.emission;
            emission.rateOverTime = 7f;
            var shape = particles.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius = .026f;
            var velocity = particles.velocityOverLifetime;
            velocity.x = new ParticleSystem.MinMaxCurve(-.10f, .10f);
            velocity.y = new ParticleSystem.MinMaxCurve(.26f, .36f);
            velocity.z = new ParticleSystem.MinMaxCurve(-.02f, .02f);
            velocity.enabled = true;
            var fade = particles.colorOverLifetime;
            fade.color = new Gradient
            {
                colorKeys = new[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(new Color(1f, .58f, .28f), 1f) },
                alphaKeys = new[] { new GradientAlphaKey(.9f, 0f), new GradientAlphaKey(0f, 1f) }
            };
            fade.enabled = true;
            embers = particles.GetComponent<ParticleSystemRenderer>();
            embers.sharedMaterial = game.particleMaterial;
            embers.maxParticleSize = .014f;
            embers.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            embers.receiveShadows = false;
        }

        private MeshRenderer AddFlameLayer(string name)
        {
            var layer = new GameObject(name, typeof(MeshFilter), typeof(MeshRenderer));
            layer.transform.SetParent(carriedFlame, false);
            layer.GetComponent<MeshFilter>().sharedMesh = flameMesh;
            var renderer = layer.GetComponent<MeshRenderer>();
            renderer.sharedMaterial = game.particleMaterial;
            renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
            renderer.receiveShadows = false;
            return renderer;
        }

        private static Mesh CreateFlameMesh()
        {
            // Each row feathers its outline to transparent while the center stays bright.
            float[] heights = { 0f, .13f, .31f, .51f, .70f, .89f, 1f };
            float[] halfWidths = { .27f, .42f, .50f, .40f, .29f, .13f, .02f };
            float[] centers = { 0f, -.025f, -.08f, -.10f, -.01f, .15f, .24f };
            float[] opacity = { .45f, .85f, .96f, .9f, .76f, .42f, 0f };
            float[] edgeOffsets = { -1f, -.54f, .54f, 1f };
            int stride = edgeOffsets.Length;
            var vertices = new Vector3[heights.Length * stride];
            var colors = new Color[vertices.Length];
            var uv = new Vector2[vertices.Length];
            var triangles = new int[(heights.Length - 1) * (stride - 1) * 6];
            int nextTriangle = 0;
            for (int row = 0; row < heights.Length; row++)
            {
                for (int col = 0; col < stride; col++)
                {
                    int index = row * stride + col;
                    vertices[index] = new Vector3(centers[row] + halfWidths[row] * edgeOffsets[col], heights[row], 0);
                    colors[index] = new Color(1f, 1f, 1f, col == 0 || col == stride - 1 ? 0f : opacity[row]);
                    uv[index] = new Vector2(.5f, .5f);
                    if (row == heights.Length - 1 || col == stride - 1) continue;
                    triangles[nextTriangle++] = index;
                    triangles[nextTriangle++] = index + stride;
                    triangles[nextTriangle++] = index + 1;
                    triangles[nextTriangle++] = index + 1;
                    triangles[nextTriangle++] = index + stride;
                    triangles[nextTriangle++] = index + stride + 1;
                }
            }

            var mesh = new Mesh
            {
                name = "Torch flame silhouette",
                vertices = vertices,
                colors = colors,
                uv = uv,
                triangles = triangles
            };
            return mesh;
        }

        private void AnimateTorchFlame()
        {
            bool burning = game.Needs.HasTorch && game.Needs.TorchLit && Inventory.Slot1.activeInHierarchy;
            foreach (var light in torchLights) light.enabled = burning;
            if (carriedFlame == null) return;
            if (carriedFlame.gameObject.activeSelf != burning) carriedFlame.gameObject.SetActive(burning);
            if (!burning) return;

            // Use the wick's animated position but render in world units and face the camera.
            var camera = View.transform;
            Vector3 towardWick = torchWick.position - camera.position;
            if (towardWick.sqrMagnitude < .001f) return;
            Vector3 facing = towardWick.normalized;
            carriedFlame.SetPositionAndRotation(torchWick.position - facing * .022f, Quaternion.LookRotation(facing, camera.up));

            float phase = Time.time * 13f;
            float flicker = Mathf.PerlinNoise(phase, 1.2f);
            float sway = (Mathf.PerlinNoise(phase * .73f, 3.4f) - .5f) * 32f;
            outerFlame.transform.localScale = new Vector3(.081f + .014f * flicker, .119f + .047f * flicker, 1f);
            outerFlame.transform.localRotation = Quaternion.Euler(0, 0, sway);
            flameCore.transform.localScale = new Vector3(.039f + .012f * flicker, .077f + .028f * flicker, 1f);
            flameCore.transform.localRotation = Quaternion.Euler(0, 0, sway * .7f);

            Vector3 viewport = View.WorldToViewportPoint(torchWick.position);
            float offsetFromAim = Mathf.Abs(viewport.x - .5f) * View.aspect;
            float visibility = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(.09f, .22f, offsetFromAim));
            TintFlame(outerFlame, new Color(1f, .34f + flicker * .11f, .035f, .92f * visibility));
            TintFlame(flameCore, new Color(1f, .86f, .31f, .95f * visibility));
            TintFlame(embers, new Color(1f, 1f, 1f, visibility));
        }

        private void TintFlame(Renderer renderer, Color color)
        {
            flameTint.SetColor(BaseColor, color);
            renderer.SetPropertyBlock(flameTint);
        }

        private void OnJump() { game.Needs.Stamina = Mathf.Max(0, game.Needs.Stamina - 16f); }
        public void SetInput(bool active)
        {
            if (!active && Usage != null) { Usage.ReleaseGrab(); Usage.CancelInteraction(); }
            if (Controller != null) Controller.enabled = active;
            if (Usage != null) Usage.enabled = active;
            if (Inventory != null) Inventory.InputEnabled = active;
        }

        private void Update()
        {
            if (game == null || !game.Running || !Controller.IsReady) return;
            var state = game.Needs;
            if (state.Stamina < 1f) exhausted = true;
            if (state.Stamina > 30f) exhausted = false;
            Controller.AllowRunning = !exhausted && !Crouched;
            Controller.AllowJumping = state.Stamina >= 16f && !Crouched;
            Controller.ConfigureSensitivity(game.sensitivity * 20f);
            state.Stamina = SurvivalState.Clamp(state.Stamina + Time.deltaTime * (Controller.IsRunning ? -18f : 15f));
            Vector3 feet = transform.position + Vector3.up * (originalCenter.y - originalHeight * 0.5f);
            bool crouch = Input.GetKey(KeyCode.C) || Input.GetKey(KeyCode.LeftControl) || Input.GetKey(KeyCode.RightControl);
            if (!crouch && Crouched)
                crouch = Physics.CheckSphere(feet + Vector3.up * 1.5f, 0.32f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
            Crouched = crouch;
            capsule.height = crouch ? 1.05f : originalHeight;
            capsule.center = originalCenter - Vector3.up * (originalHeight - capsule.height) * 0.5f;
            float speed = Crouched ? 0.55f : 1f;
            bool gale = game.Weather.OnRidge(feet) && game.Weather.Gusting && !game.Sheltered;
            Controller.MovementSpeedMultiplier = gale ? speed * 0.65f : speed;
            Controller.ExternalVelocity = gale ? Vector3.right * (Crouched ? 0.12f : 1.0f) : Vector3.zero;
        }

        private void LateUpdate()
        {
            if (game == null) return;
            if (game.Running && Controller.IsReady)
            {
                Vector3 eye = View.transform.localPosition;
                eye.y = Mathf.Lerp(eye.y, originalEye - (Crouched ? originalHeight - 1.05f : 0f), Time.deltaTime * 12f);
                View.transform.localPosition = eye;
                shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
                if (shake > 0f) View.transform.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 55f) * shake * 2f, 0, 0);
            }
            AnimateTorchFlame();
        }

        public void Teleport(Vector3 feet)
        {
            Crouched = false; capsule.height = originalHeight; capsule.center = originalCenter;
            View.transform.localPosition = new Vector3(View.transform.localPosition.x, originalEye, View.transform.localPosition.z);
            Controller.TeleportFeet(feet); Usage.ReleaseGrab();
        }
        public void ResetMotion() { exhausted = false; Usage.CancelInteraction(); }
        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }
        private void OnDestroy()
        {
            if (Controller != null) Controller.Jumped -= OnJump;
            if (flameMesh != null) Destroy(flameMesh);
        }
    }
}
