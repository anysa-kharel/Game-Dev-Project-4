using UnityEngine;
using UnityEngine.UI;
using StormSurvival;

public class ObjectUsage : MonoBehaviour
{
    [Header ("Fuel Objects")]
    public Slider fuel;
    public OilHolder OilHolder;
    public GameObject TakeOilPanel;
    public Slider OilBarrelStats;
    public GameObject refillText;

    [Header ("Lamp Objects")]
    public GameObject LampInfo;
    public Text LampStatus;

    [Header ("Eat and Drink Objects")]
    public LifeStats stats;
    public GameObject EatPanel;
    public Slider EatSlider;
    public GameObject DrinkPanel;


    [Header ("Punch Objects")]
    public HandInventory inventory;
    public Animator punchAnim;
    public GameObject pickaxeSparks;

    [Header ("Grabbing Objects")]
    public GameObject GrabIcon;
    public SpringJoint joint;
    bool grabbing = false;

    [Header("Generator Objects")]
    public GameObject GeneratorPanel;
    public GameObject RefillPanelGenerator;
    public Slider GenFuelSlider;
    public Text GeneratorStatus;

    [Header("Spotlight Objects")]
    public GameObject SpotlightPanel;
    public Slider SpotDistanceSlider;
    public Text SpotlightStatus;


    [Header ("Other objects")]
    public Camera PlayerCam;













    public string CurrentPrompt { get; private set; }
    public float HoldProgress { get; private set; }
    private StormInteractable holding;
    private float nextSwing;
    private float grabSpring = 30f;
    private void Start()
    {
        if (PlayerCam == null) PlayerCam = GetComponentInChildren<Camera>();
        if (stats == null) stats = GetComponent<LifeStats>();
        if (inventory == null) inventory = GetComponent<HandInventory>();
        if (OilHolder == null) OilHolder = GetComponent<OilHolder>();
        if (joint != null) { grabSpring = Mathf.Max(30f, joint.spring); ReleaseGrab(); }
    }
    private void Update()
    {
        if (Time.timeScale <= 0f || PlayerCam == null || inventory == null) return;
        HidePanels(); CurrentPrompt = "";
        bool hitSomething = Physics.Raycast(PlayerCam.transform.position, PlayerCam.transform.forward,
            out RaycastHit hit, 4f, Physics.DefaultRaycastLayers, QueryTriggerInteraction.Ignore);
        bool swing = Input.GetMouseButtonDown(0) && Time.time >= nextSwing;
        if (swing)
        {
            nextSwing = Time.time + 0.55f;
            if (punchAnim != null) punchAnim.SetTrigger(inventory.selected == 2 ? "PunchPick" : "PunchTorch");
        }
        if (!Input.GetMouseButton(1)) ReleaseGrab();
        if (!hitSomething) { CancelInteraction(); return; }
        var adventure = hit.collider.GetComponentInParent<StormInteractable>();
        if (adventure != null && adventure.Available)
        {
            CurrentPrompt = adventure.Prompt;
            if (adventure.IsChoppable)
            {
                if (inventory.selected != 2) CurrentPrompt = "Press 3 to equip the pickaxe";
                else if (swing) adventure.Chop();
                CancelHold(); return;
            }
            if (Input.GetKey(KeyCode.E))
            {
                if (holding != adventure) { holding = adventure; HoldProgress = 0; }
                HoldProgress += Time.deltaTime / adventure.HoldSeconds;
                if (HoldProgress >= 1f) { adventure.Use(); CancelHold(); }
            }
            else CancelHold();
            return;
        }
        CancelHold();
        var mine = hit.collider.GetComponentInParent<Mineable>();
        if (mine != null)
        {
            CurrentPrompt = inventory.selected == 2 ? "Left click / mine rock" : "Press 3 / equip pickaxe";
            if (swing && inventory.selected == 2)
            {
                mine.Health -= 50f; mine.MineRefresh(); mine.Save();
                if (pickaxeSparks != null) Destroy(Instantiate(pickaxeSparks, hit.point, Quaternion.LookRotation(hit.normal)), 2f);
            }
            return;
        }
        var ai = hit.collider.GetComponentInParent<AIController>();
        if (ai != null && swing) { ai.Health -= inventory.selected == 2 ? 50f : 20f; ai.Agressive = true; }
        if (hit.distance > 3.0f) return;
        bool use = Input.GetKeyDown(KeyCode.E), refill = Input.GetKeyDown(KeyCode.R);
        var gen = hit.collider.GetComponentInParent<Generator>();
        var lamp = hit.collider.GetComponentInParent<Lantern>();
        var barrel = hit.collider.GetComponentInParent<OilBarrel>();
        var food = hit.collider.GetComponentInParent<Eatable>();
        var water = hit.collider.GetComponentInParent<WaterDrinkable>();
        var spotlight = hit.collider.GetComponentInParent<Spotlight>();
        if (gen != null)
        {
            CurrentPrompt = "E / " + (gen.Toggled ? "stop generator" : "start generator") + "   |   2 then R / refill";
            Show(GeneratorPanel); Value(GenFuelSlider, gen.CurrentFuel, gen.MaxFuel);
            if (GeneratorStatus != null) GeneratorStatus.text = gen.Toggled ? "Running" : gen.CurrentFuel > 0 ? "Stopped" : "Out of fuel";
            if (use) { gen.Toggle(); if (!gen.Toggled && gen.CurrentFuel <= 0) Tell("The generator needs fuel. Select the cup with 2, then press R."); }
            if (refill && inventory.selected == 1 && OilHolder != null && OilHolder.HoldingNow > 0 && gen.CurrentFuel < gen.MaxFuel - 1)
            { OilHolder.HoldingNow--; gen.CurrentFuel = Mathf.Min(gen.MaxFuel, gen.CurrentFuel + 1000); OilHolder.Save(); gen.SaveStats(); Tell("Generator refilled. Press E to start it."); }
        }
        else if (barrel != null)
        {
            CurrentPrompt = "2 / equip fuel cup   |   E / take fuel (" + Mathf.FloorToInt(barrel.fuel) + " left)";
            Show(TakeOilPanel); Value(OilBarrelStats, barrel.fuel, barrel.MaxFuel);
            if (use && OilHolder != null)
            {
                if (inventory.selected != 1) Tell("Select the fuel cup with 2 first.");
                else
                {
                    int taken = Mathf.Min(2 - OilHolder.HoldingNow, Mathf.FloorToInt(barrel.fuel));
                    if (taken > 0) { OilHolder.HoldingNow += taken; barrel.fuel -= taken; OilHolder.Save(); barrel.Save(); Tell("Fuel cup filled. Use R at a generator or lantern."); }
                    else Tell(OilHolder.HoldingNow >= 2 ? "The cup is full." : "This fuel supply is empty.");
                }
            }
        }
        else if (food != null && food.FoodAmount > 1)
        {
            CurrentPrompt = "E / eat from the can (" + Mathf.CeilToInt(food.FoodAmount - 1) + " servings)";
            Show(EatPanel); Value(EatSlider, food.FoodAmount, food.MaxFood);
            if (use && stats != null && (stats.Hunger < 950 || stats.Thirst < 950 || stats.Health < 950))
            { stats.Hunger = Mathf.Min(1000, stats.Hunger + food.SaturationAmount); stats.Thirst = Mathf.Min(1000, stats.Thirst + food.WaterAmount); stats.Health = Mathf.Min(1000, stats.Health + 100); food.FoodAmount--; food.Save(); Tell("Food restores hunger, thirst, and a little health."); }
        }
        else if (water != null)
        {
            CurrentPrompt = "E / drink water (" + water.WaterAmount + ")"; Show(DrinkPanel);
            if (use && water.WaterAmount > 0 && stats != null && stats.Thirst < 950)
            { stats.Thirst = Mathf.Min(1000, stats.Thirst + 250); water.WaterAmount--; water.Save(); Tell("Thirst restored."); }
        }
        else if (lamp != null)
        {
            CurrentPrompt = "E / toggle lantern   |   2 then R / refill"; Show(LampInfo); Value(fuel, lamp.Fuel, 1000);
            if (LampStatus != null) LampStatus.text = lamp.toggled ? "Active" : "Off";
            if (use) lamp.Toggle();
            if (refill && inventory.selected == 1 && OilHolder != null && OilHolder.HoldingNow > 0 && lamp.Fuel < 950)
            { OilHolder.HoldingNow--; lamp.Fuel = Mathf.Min(1000, lamp.Fuel + 900); OilHolder.Save(); lamp.SaveSettings(); Tell("Lantern refilled."); }
        }
        else if (spotlight != null)
        {
            CurrentPrompt = "E / toggle spotlight"; Show(SpotlightPanel); Value(SpotDistanceSlider, spotlight.distance, spotlight.MaxDistance);
            if (SpotlightStatus != null) SpotlightStatus.text = spotlight.Toggled ? "Enabled" : "Disabled";
            if (use) spotlight.Toggle();
        }
        var rigid = hit.rigidbody;
        if (rigid != null && !rigid.isKinematic && joint != null)
        {
            Show(GrabIcon);
            if (Input.GetMouseButton(1)) { joint.connectedBody = rigid; joint.connectedAnchor = rigid.transform.InverseTransformPoint(hit.point); joint.spring = grabSpring; grabbing = true; }
        }
    }
    private void Tell(string message) { if (StormGame.Instance != null) StormGame.Instance.Toast(message, 5f); }
    public void CancelInteraction() { CurrentPrompt = ""; CancelHold(); }
    private void CancelHold() { holding = null; HoldProgress = 0f; }
    public void ReleaseGrab() { if (joint != null) { joint.spring = 0f; joint.connectedBody = null; } grabbing = false; }
    private void Show(GameObject panel) { if (panel != null) panel.SetActive(true); }
    private void Value(Slider slider, float value, float maximum) { if (slider != null) { slider.maxValue = maximum; slider.value = value; } }
    private void HidePanels()
    {
        foreach (var panel in new[] { TakeOilPanel, refillText, LampInfo, EatPanel, DrinkPanel, GrabIcon, GeneratorPanel, RefillPanelGenerator, SpotlightPanel })
            if (panel != null) panel.SetActive(false);
    }
}
