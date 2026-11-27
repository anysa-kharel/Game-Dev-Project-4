using UnityEngine;

namespace StormSurvival
{
    public sealed class StormWeather : MonoBehaviour
    {
        private const float WarningDuration = 2.6f;
        private StormGustSchedule gusts;
        private bool ridgeWeatherStarted;

        public float Intensity { get; private set; } = 0.15f;
        public bool Gusting => gusts != null && gusts.Gusting;
        public float UntilGust => gusts != null ? gusts.UntilGust : float.PositiveInfinity;
        public bool GustWarning => !Gusting && UntilGust <= WarningDuration;
        public float GustStrength => gusts != null ? gusts.Strength : 0f;
        public int WindDirection => gusts != null ? gusts.Direction : 1;
        private StormGame game;
        private ParticleSystem rain;
        private ParticleSystemRenderer rainRenderer;
        private float lightning, nextThunder = 25f;
        public bool OnRidge(Vector3 position) => position.z > 180f && position.z < 236f;
        public void Initialize(StormGame owner)
        {
            game = owner;
            gusts = new StormGustSchedule(owner.RunLayout.WindSeed);
            var obj = new GameObject("Rain around the survivor"); obj.transform.rotation = Quaternion.Euler(90,0,0);
            rain = obj.AddComponent<ParticleSystem>();
            var main = rain.main; main.startLifetime = 1.2f; main.startSpeed = 15f; main.startSize = 0.035f;
            main.startColor = new Color(.60f,.73f,.76f,.36f); main.maxParticles = 2400; main.simulationSpace = ParticleSystemSimulationSpace.World;
            var shape = rain.shape; shape.shapeType = ParticleSystemShapeType.Box; shape.scale = new Vector3(30,30,.1f);
            var emission = rain.emission; emission.rateOverTime = 650f;
            rainRenderer = obj.GetComponent<ParticleSystemRenderer>();rainRenderer.sharedMaterial = game.particleMaterial;
            rainRenderer.renderMode = ParticleSystemRenderMode.Stretch; rainRenderer.lengthScale = 4f; rainRenderer.velocityScale = .08f;
        }
        private void Update()
        {
            if (game == null || !game.Running) return;
            Intensity = Mathf.Clamp01(.15f + game.Elapsed / 500f * .55f + Mathf.Clamp01(game.Player.transform.position.z / 260f) * .30f);
            Vector3 position = game.Player.transform.position;
            bool nearRidge = position.z >= 176f && position.z <= 238f;
            if (nearRidge) ridgeWeatherStarted = true;
            if (ridgeWeatherStarted) gusts.Advance(Time.deltaTime);
            float windCue = Gusting ? Mathf.Clamp01(GustStrength) : Mathf.Clamp01(1f - UntilGust / WarningDuration);
            game.World.SwayRidgeTrees(nearRidge ? .12f + windCue * .88f : 0f, WindDirection);
            game.Audio.SetRidgeWind(nearRidge ? windCue * (game.Sheltered ? .35f : 1f) : 0f);
            rain.transform.position = position + Vector3.up * 9f;
            rainRenderer.enabled = !game.Sheltered;
            var emission = rain.emission; emission.rateOverTime = Mathf.Lerp(500,1600,Intensity);
            RenderSettings.fogDensity = Mathf.Lerp(.010f,.017f,Intensity);
            RenderSettings.fogColor = Color.Lerp(new Color(.33f,.40f,.40f),new Color(.23f,.30f,.32f),Intensity);
            game.Player.View.backgroundColor = RenderSettings.fogColor;
            if (game.Elapsed > nextThunder)
            {
                nextThunder = game.Elapsed + 24f + Mathf.PingPong(game.Elapsed,11f);
                lightning = .20f; game.Audio.Play("thunder",game.Player.transform.position,.45f);
            }
            lightning = Mathf.Max(0f,lightning-Time.deltaTime);
            game.World.Sun.intensity = lightning > 0f ? 2.8f : Mathf.Lerp(1.15f,.7f,Intensity);
        }
    }
}
