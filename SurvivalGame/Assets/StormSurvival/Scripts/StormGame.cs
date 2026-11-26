using UnityEngine;
using UnityEngine.SceneManagement;

namespace StormSurvival
{
    public enum StormMode { Title, Playing, Paused, Crafting, Failed, Won }
    public sealed class StormGame : MonoBehaviour
    {
        public GameObject[] trees, rocks;
        public GameObject playerTemplate, foodTemplate, waterTemplate, fuelTemplate;
        public GameObject generatorTemplate, spotlightTemplate, lanternTemplate, mineTemplate, miningRockTemplate;
        public Generator RescueGenerator { get; set; }
        public Texture2D groundTexture, woodTexture;
        public Material surfaceMaterial, glowMaterial, particleMaterial;
        public Material crawlspaceStoneMaterial;
        public AudioClip templateMusic;
        [Range(0.03f, 0.25f)] public float sensitivity = 0.10f;
        [Range(0f, 1f)] public float volume = 0.65f;
        public static StormGame Instance { get; private set; }
        public SurvivalState Needs { get; private set; } = new SurvivalState();
        public StormMode Mode { get; private set; } = StormMode.Title;
        public bool Running => Mode == StormMode.Playing;
        public StormWorld World { get; private set; }
        public StormPlayer Player { get; private set; }
        public StormUI UI { get; private set; }
        public StormAudio Audio { get; private set; }
        public StormWeather Weather { get; private set; }
        public float Elapsed { get; private set; }
        public int Retries { get; private set; }
        public int WoodGathered { get; private set; }
        public string Notice { get; private set; }
        public float NoticeUntil { get; private set; }
        public string CheckpointName { get; private set; } = "Forest trailhead";
        public bool Sheltered { get; private set; }
        public bool Warming { get; private set; }
        private Vector3 checkpoint = new Vector3(0, 0.2f, 3);
        private StormInteractable checkpointFire;
        private float waterCooldown;

        public string Objective
        {
            get
            {
                if (!Needs.HasPickaxe) return "Select the pickaxe with 3.";
                if (!Needs.FirstFire)
                    return Needs.Wood >= 3 && Needs.Stone >= 2
                        ? "Hold E at the campfire ring to build a fire."
                        : "Gather 3 wood and 2 stone with the pickaxe.";
                if (!Needs.HasTorch) return "Press Tab to craft a torch: 2 wood + 1 cloth.";
                if (!Needs.TorchLit) return "Light your torch at a burning campfire. Rewrap it if its fuel is empty.";
                if (!Needs.BridgeBuilt) return "Follow the trail through the logging camp. Mine the marked bridge obstruction at the river.";
                if (Player != null && Player.transform.position.z < 178f) return "Cross the trunk bridge. Warm up at the far-bank camp.";
                if (Player != null && Player.transform.position.z < 236f) return "Reach the lookout. Use the windbreaks when a gust rises.";
                if (RescueGenerator == null || RescueGenerator.CurrentFuel <= 0f) return "Take fuel from the barrel with 2 then E. Refill the lookout generator with R.";
                if (!RescueGenerator.Toggled) return "Press E at the refilled generator to power the lookout.";
                return "Light the rescue beacon: 6 wood + a burning torch + generator power.";
            }
        }

        public Vector3 ObjectivePosition
        {
            get
            {
                
                if (!Needs.FirstFire || !Needs.HasTorch) return new Vector3(-5, 0, 24);
                if (!Needs.TorchLit) return World.NearestFire(Player.transform.position).transform.position;
                if (!Needs.BridgeBuilt) return new Vector3(2, 0, 125);
                if (Player.transform.position.z > 236f && RescueGenerator != null && (!RescueGenerator.Toggled || RescueGenerator.CurrentFuel <= 0)) return RescueGenerator.transform.position;
                return new Vector3(0, 18, 258);
            }
        }

        private void Awake()
        {
            Instance = this; TemplatePrefs.Enabled = false;
            Needs.HasPickaxe = true; Needs.Cloth = 1;
            Time.timeScale = 1f; Application.targetFrameRate = 90;
            Audio = gameObject.AddComponent<StormAudio>(); Audio.Initialize(this);
            World = gameObject.AddComponent<StormWorld>(); World.Build(this);
            Player = World.CreatePlayer();
            Weather = gameObject.AddComponent<StormWeather>(); Weather.Initialize(this);
            UI = gameObject.AddComponent<StormUI>(); UI.Build(this);
            SetMode(StormMode.Title);
        }

        private void Update()
        {
            if (Input.GetKeyDown(KeyCode.Escape))
            {
                if (Mode == StormMode.Playing) SetMode(StormMode.Paused);
                else if (Mode == StormMode.Crafting || Mode == StormMode.Paused) SetMode(StormMode.Playing);
            }
            if (Input.GetKeyDown(KeyCode.Tab))
            {
                if (Mode == StormMode.Playing) SetMode(StormMode.Crafting);
                else if (Mode == StormMode.Crafting) SetMode(StormMode.Playing);
            }
            if (Input.GetKeyDown(KeyCode.Return) && Mode == StormMode.Title) Begin();
            if (Input.GetKeyDown(KeyCode.R) && Mode == StormMode.Failed) Retry();
            if (!Running) return;
            Elapsed += Time.deltaTime;
            waterCooldown -= Time.deltaTime;
            Sheltered = World.IsSheltered(Player.transform.position);
            Warming = World.NearBurningFire(Player.transform.position);
            if (!Needs.Rescued) Player.Stats.Tick(Time.deltaTime);
            Needs.Health = Player.Stats.Health * 0.1f;
            Needs.Step(Time.deltaTime, Sheltered, Warming, Weather.Intensity, Player.Inventory.selected == 0);
            Vector3 position = Player.transform.position;
            if (waterCooldown <= 0f && position.z > 129f && position.z < 143f && position.y < -0.45f)
            {
                waterCooldown = 3f;
                Needs.Damage(24f); Needs.Wetness = 100f; Needs.Warmth = Mathf.Max(0, Needs.Warmth - 22f);
                float shore = Needs.BridgeBuilt && position.z > 136f ? 146f : 126f;
                Player.Teleport(new Vector3(0, StormWorld.Height(shore) + 0.2f, shore));
                Audio.Play("water", position, 0.8f);
                Toast("The current swept you ashore. You are soaked. Find shelter or fire.", 7f);
            }
            if (position.y < -12f) Needs.Damage(100f);
            Player.Stats.Health = Needs.Health * 10f;
            if (!Needs.Alive) { Audio.Play("failure", position, 0.7f); SetMode(StormMode.Failed); }
        }

        public void Begin() { SetMode(StormMode.Playing); Toast("Follow the amber trail markers.", 5f); }
        public void SetMode(StormMode mode)
        {
            Mode = mode;
            if (Player != null) Player.SetInput(Running);
            Time.timeScale = Running ? 1f : 0f;
            Cursor.lockState = Running ? CursorLockMode.Locked : CursorLockMode.None;
            Cursor.visible = !Running;
            if (UI != null) UI.RefreshMode();
        }
        public void CraftTorch()
        {
            bool already = Needs.HasTorch;
            if (!Needs.CraftTorch()) { Toast("Need " + (already ? "1 wood and room for fuel." : "2 wood + 1 cloth.")); if (UI != null) UI.RefreshMode(); return; }
            Player.Inventory.selected = 0; Player.Inventory.Refresh();
            Audio.Play("craft", Player.transform.position, 0.5f);
            Toast(already ? "Torch rewrapped: two more minutes of fuel." : "Torch crafted. Hold E at a lit campfire to light it.", 6f);
            UI.RefreshMode();
        }
        public void SetCheckpoint(StormInteractable fire)
        {
            checkpointFire = fire;
            checkpoint = fire.transform.position + new Vector3(0, 0.2f, -4f);
            checkpoint.y = StormWorld.Height(checkpoint.z) + 0.2f;
            CheckpointName = fire.label;
            Audio.Play("checkpoint", checkpoint, 0.5f);
            Toast("CHECKPOINT  /  " + CheckpointName + "\nRest by the fire to dry out and warm up.", 7f);
        }
        public void Retry()
        {
            Retries++; Needs.Recover(); Player.Stats.Health = 1000;
            Player.Stats.Hunger = Mathf.Max(600, Player.Stats.Hunger); Player.Stats.Thirst = Mathf.Max(800, Player.Stats.Thirst);
            if (checkpointFire != null) checkpointFire.fuel = Mathf.Max(checkpointFire.fuel, 100f);
            Player.Teleport(checkpoint); Player.ResetMotion();
            SetMode(StormMode.Playing);
            Toast("Restored at " + CheckpointName + ". Supplies and opened routes retained.", 6f);
        }
        public void Win()
        {
            if (!Running || Needs.Rescued) return;
            Needs.Rescued = true; Audio.Play("success", Player.transform.position, 0.8f);
            Toast("The beacon is burning. Your rescue signal is through.", 5f);
            StartCoroutine(CompleteRescue());
        }
        private System.Collections.IEnumerator CompleteRescue()
        { yield return new WaitForSeconds(2.5f); SetMode(StormMode.Won); }
        public void Restart() { Time.timeScale = 1f; SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex); }
        public void AddWood(int amount) { Needs.Wood += amount; WoodGathered += amount; Audio.Play("collect", Player.transform.position, 0.4f); Toast("+" + amount + " wood  /  " + Needs.Wood + " carried"); }
        public void Toast(string message, float seconds = 4f) { Notice = message; NoticeUntil = Time.unscaledTime + seconds; }
        private void OnApplicationFocus(bool focused) { if (!focused && Running) SetMode(StormMode.Paused); }
        private void OnDestroy() { TemplatePrefs.Enabled = true; Time.timeScale = 1f; Cursor.lockState = CursorLockMode.None; Cursor.visible = true; if (Instance == this) Instance = null; }
    }
}
