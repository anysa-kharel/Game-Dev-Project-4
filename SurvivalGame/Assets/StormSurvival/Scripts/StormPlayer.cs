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
        private ParticleSystem[] flames;
        private Light[] torchLights;

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
            flames = Inventory.Slot1.GetComponentsInChildren<ParticleSystem>(true);
            torchLights = Inventory.Slot1.GetComponentsInChildren<Light>(true);
            ConfigureTorchEffects();
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

        private void ConfigureTorchEffects()
        {
            // The legacy fire uses large, long-lived flipbook sprites. At first-person
            // distance these cover the camera. Keep a compact effect at the torch tip.
            foreach (var flame in flames)
            {
                if (flame == null) continue;
                flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
                bool spark = flame.name.IndexOf("Spark", System.StringComparison.OrdinalIgnoreCase) >= 0;
                var main = flame.main;
                main.playOnAwake = false; main.prewarm = false; main.loop = true;
                main.simulationSpace = ParticleSystemSimulationSpace.Local;
                // Parent/animation scale may position the emitter, but must not enlarge sprites.
                main.scalingMode = ParticleSystemScalingMode.Shape;
                main.startSize3D = false; main.startRotation3D = false;
                main.startSize = spark ? new ParticleSystem.MinMaxCurve(.008f, .015f) : new ParticleSystem.MinMaxCurve(.035f, .07f);
                main.startLifetime = spark ? new ParticleSystem.MinMaxCurve(.12f, .22f) : new ParticleSystem.MinMaxCurve(.18f, .30f);
                main.startSpeed = 0f; main.startRotation = 0f; main.gravityModifier = 0f;
                main.startColor = spark ? new Color(1f, .76f, .30f, .70f) : new Color(1f, .65f, .25f, .80f);
                main.maxParticles = spark ? 5 : 12;
                var emission = flame.emission; emission.enabled = true; emission.rateOverTime = spark ? 5f : 20f;
                emission.SetBursts(new ParticleSystem.Burst[0]);
                var shape = flame.shape; shape.enabled = true; shape.shapeType = ParticleSystemShapeType.Sphere; shape.radius = .018f;
                var velocity = flame.velocityOverLifetime;
                velocity.enabled = false;
                velocity.space = ParticleSystemSimulationSpace.World;
                velocity.x = new ParticleSystem.MinMaxCurve(0f, 0f);
                velocity.y = spark ? new ParticleSystem.MinMaxCurve(.20f, .35f) : new ParticleSystem.MinMaxCurve(.16f, .26f);
                velocity.z = new ParticleSystem.MinMaxCurve(0f, 0f);
                velocity.enabled = true;
                var size = flame.sizeOverLifetime; size.enabled = true; size.separateAxes = false;
                size.size = new ParticleSystem.MinMaxCurve(1f, AnimationCurve.EaseInOut(0f, 1f, 1f, .15f));
                var tint = new Gradient();
                tint.SetKeys(new[] { new GradientColorKey(new Color(1f, .85f, .35f), 0f), new GradientColorKey(new Color(1f, .25f, .03f), 1f) },
                    new[] { new GradientAlphaKey(.7f, 0f), new GradientAlphaKey(.45f, .35f), new GradientAlphaKey(0f, 1f) });
                var colors = flame.colorOverLifetime; colors.enabled = true; colors.color = tint;
                var noise = flame.noise; noise.enabled = false;
                var sheet = flame.textureSheetAnimation; sheet.enabled = false;
                var sizeSpeed = flame.sizeBySpeed; sizeSpeed.enabled = false;
                var colorSpeed = flame.colorBySpeed; colorSpeed.enabled = false;
                var force = flame.forceOverLifetime; force.enabled = false;
                var trails = flame.trails; trails.enabled = false;
                var renderer = flame.GetComponent<ParticleSystemRenderer>();
                if (renderer == null) continue;
                renderer.sharedMaterial = game.particleMaterial;
                renderer.renderMode = ParticleSystemRenderMode.Billboard;
                renderer.minParticleSize = 0f; renderer.maxParticleSize = spark ? .012f : .045f;
                renderer.sortingFudge = 0f; renderer.cameraVelocityScale = 0f;
                renderer.velocityScale = 0f; renderer.lengthScale = 1f;
                renderer.shadowCastingMode = UnityEngine.Rendering.ShadowCastingMode.Off;
                renderer.receiveShadows = false;
            }
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
            bool burning = game.Needs.TorchLit && Inventory.selected == 0 && game.Needs.HasTorch;
            foreach (var light in torchLights) if (light != null) light.enabled = burning;
            foreach (var flame in flames)
            {
                if (flame == null) continue;
                if (burning && !flame.isPlaying && flame.gameObject.activeInHierarchy) flame.Play();
                else if (!burning && flame.isPlaying) flame.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
            }
            if (!game.Running || !Controller.IsReady) return;
            Vector3 eye = View.transform.localPosition;
            eye.y = Mathf.Lerp(eye.y, originalEye - (Crouched ? originalHeight - 1.05f : 0f), Time.deltaTime * 12f);
            View.transform.localPosition = eye;
            shake = Mathf.MoveTowards(shake, 0f, Time.deltaTime);
            if (shake > 0f) View.transform.localRotation *= Quaternion.Euler(Mathf.Sin(Time.time * 55f) * shake * 2f, 0, 0);
        }

        public void Teleport(Vector3 feet)
        {
            Crouched = false; capsule.height = originalHeight; capsule.center = originalCenter;
            View.transform.localPosition = new Vector3(View.transform.localPosition.x, originalEye, View.transform.localPosition.z);
            Controller.TeleportFeet(feet); Usage.ReleaseGrab();
        }
        public void ResetMotion() { exhausted = false; Usage.CancelInteraction(); }
        public void Shake(float amount) { shake = Mathf.Max(shake, amount); }
        private void OnDestroy() { if (Controller != null) Controller.Jumped -= OnJump; }
    }
}
