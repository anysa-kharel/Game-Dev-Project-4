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













    private const float HarvestReach = 4f;
    private const float UseReach = 3f;
    private const float AimRadius = .32f;
    private const float PickaxeContactTime = .40f;
    private static readonly int PickaxeSwing = Animator.StringToHash("Base Layer.PickaxePunch");
    private readonly RaycastHit[] aimHits = new RaycastHit[32];

    public string CurrentPrompt { get; private set; }
    public float HoldProgress { get; private set; }
    private StormInteractable holding;
    private StormSupplySled heldSled;
    private Component pendingHarvest;
    private float harvestContactAt;
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
        HidePanels();
        CurrentPrompt = string.Empty;

        bool grabHeld = Input.GetMouseButton(1);
        if (!grabHeld) ReleaseGrab();
        else if (heldSled != null)
        {
            if (heldSled.Pull(transform.position))
            {
                CurrentPrompt = heldSled.Prompt;
                CancelHold();
                return;
            }
            ReleaseGrab();
        }

        var view = PlayerCam.transform;
        var target = FindTarget(new Ray(view.position, view.forward), out var contact, out var directHit);
        ApplyHarvestContact(target, contact);
        bool swing = Input.GetMouseButtonDown(0) && Time.time >= nextSwing;
        if (swing)
        {
            BeginSwing(target);
            ApplyHarvestContact(target, contact);
        }

        if (target is StormWolf wolf)
        {
            CancelHold();
            if (wolf.CanTarget)
                CurrentPrompt = inventory.selected == 2 ? "Left click / ward off wolf" : wolf.Prompt;
            return;
        }

        if (target is StormSupplySled sled)
        {
            CancelHold();
            if (grabHeld && sled.BeginPull(transform.position))
            {
                ReleaseGrab();
                heldSled = sled;
            }
            if (Input.GetKeyDown(KeyCode.E)) sled.Use();
            CurrentPrompt = sled.CanTarget ? sled.Prompt : string.Empty;
            return;
        }

        if (target is StormSearchlight searchlight)
        {
            CancelHold();
            if (Input.GetKeyDown(KeyCode.E)) searchlight.Rotate();
            CurrentPrompt = searchlight.Prompt;
            return;
        }

        if (target is StormInteractable adventure)
        {
            if (!adventure.Available) { CancelHold(); return; }
            CurrentPrompt = adventure.Prompt;
            if (adventure.IsChoppable)
            {
                if (inventory.selected != 2) CurrentPrompt = "3 / equip pickaxe";
                CancelHold();
            }
            else if (Input.GetKey(KeyCode.E))
            {
                if (holding != adventure) { holding = adventure; HoldProgress = 0; }
                HoldProgress += Time.deltaTime / adventure.HoldSeconds;
                if (HoldProgress >= 1f) { adventure.Use(); CancelHold(); }
            }
            else CancelHold();
            return;
        }

        CancelHold();
        if (target is Mineable mine)
        {
            if (mine.CanMine)
                CurrentPrompt = inventory.selected == 2 ? "Left click / mine rock" : "3 / equip pickaxe";
            return;
        }

        if (directHit.collider != null && swing)
        {
            var ai = directHit.collider.GetComponentInParent<AIController>();
            if (ai != null)
            {
                ai.Health -= inventory.selected == 2 ? 50f : 20f;
                ai.Agressive = true;
            }
        }

        bool use = Input.GetKeyDown(KeyCode.E), refill = Input.GetKeyDown(KeyCode.R);
        if (target is Generator gen)
        {
            CurrentPrompt = "E / " + (gen.Toggled ? "stop generator" : "start generator") + "   |   2 then R / refill";
            Show(GeneratorPanel); Value(GenFuelSlider, gen.CurrentFuel, gen.MaxFuel);
            if (GeneratorStatus != null) GeneratorStatus.text = gen.Toggled ? "Running" : gen.CurrentFuel > 0 ? "Stopped" : "Out of fuel";
            if (use) { gen.Toggle(); if (!gen.Toggled && gen.CurrentFuel <= 0) Tell("The generator needs fuel. Select the cup with 2, then press R."); }
            if (refill && inventory.selected == 1 && OilHolder != null && OilHolder.HoldingNow > 0 && gen.CurrentFuel < gen.MaxFuel - 1)
            { OilHolder.HoldingNow--; gen.CurrentFuel = Mathf.Min(gen.MaxFuel, gen.CurrentFuel + 1000); OilHolder.Save(); gen.SaveStats(); Tell("Generator refilled. Press E to start it."); }
        }
        else if (target is OilBarrel barrel)
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
        else if (target is Eatable food)
        {
            CurrentPrompt = "E / eat from the can (" + Mathf.CeilToInt(food.FoodAmount - 1) + " servings)";
            Show(EatPanel); Value(EatSlider, food.FoodAmount, food.MaxFood);
            if (use && stats != null && (stats.Hunger < 950 || stats.Thirst < 950 || stats.Health < 950))
            { stats.Hunger = Mathf.Min(1000, stats.Hunger + food.SaturationAmount); stats.Thirst = Mathf.Min(1000, stats.Thirst + food.WaterAmount); stats.Health = Mathf.Min(1000, stats.Health + 100); food.FoodAmount--; food.Save(); Tell("Food restores hunger, thirst, and a little health."); }
        }
        else if (target is WaterDrinkable water)
        {
            CurrentPrompt = "E / drink water (" + water.WaterAmount + ")"; Show(DrinkPanel);
            if (use && water.WaterAmount > 0 && stats != null && stats.Thirst < 950)
            { stats.Thirst = Mathf.Min(1000, stats.Thirst + 250); water.WaterAmount--; water.Save(); Tell("Thirst restored."); }
        }
        else if (target is Lantern lamp)
        {
            CurrentPrompt = "E / toggle lantern   |   2 then R / refill"; Show(LampInfo); Value(fuel, lamp.Fuel, 1000);
            if (LampStatus != null) LampStatus.text = lamp.toggled ? "Active" : "Off";
            if (use) lamp.Toggle();
            if (refill && inventory.selected == 1 && OilHolder != null && OilHolder.HoldingNow > 0 && lamp.Fuel < 950)
            { OilHolder.HoldingNow--; lamp.Fuel = Mathf.Min(1000, lamp.Fuel + 900); OilHolder.Save(); lamp.SaveSettings(); Tell("Lantern refilled."); }
        }
        else if (target is Spotlight spotlight)
        {
            CurrentPrompt = "E / toggle spotlight"; Show(SpotlightPanel); Value(SpotDistanceSlider, spotlight.distance, spotlight.MaxDistance);
            if (SpotlightStatus != null) SpotlightStatus.text = spotlight.Toggled ? "Enabled" : "Disabled";
            if (use) spotlight.Toggle();
        }
        var rigid = directHit.distance <= UseReach ? directHit.rigidbody : null;
        if (rigid != null && !rigid.isKinematic && joint != null)
        {
            Show(GrabIcon);
            if (Input.GetMouseButton(1)) { joint.connectedBody = rigid; joint.connectedAnchor = rigid.transform.InverseTransformPoint(directHit.point); joint.spring = grabSpring; grabbing = true; }
        }
    }
    private void BeginSwing(Component target)
    {
        nextSwing = Time.time + .55f;
        bool pickaxe = inventory.selected == 2;
        bool animated = punchAnim != null && punchAnim.isActiveAndEnabled;
        if (animated)
        {
            if (pickaxe && punchAnim.HasState(0, PickaxeSwing)) punchAnim.Play(PickaxeSwing, 0, 0f);
            else punchAnim.SetTrigger(pickaxe ? "PunchPick" : "PunchTorch");
        }

        if (pickaxe && (target is Mineable || target is StormWolf || target is StormInteractable { IsChoppable: true }))
        {
            pendingHarvest = target;
            harvestContactAt = Time.time + (animated ? PickaxeContactTime : 0f);
        }
    }

    private void ApplyHarvestContact(Component target, RaycastHit contact)
    {
        if (pendingHarvest == null || Time.time < harvestContactAt) return;

        var receiver = pendingHarvest;
        pendingHarvest = null;
        if (inventory.selected != 2 || target != receiver) return;

        if (receiver is Mineable rock) rock.Strike(50f, contact.point, contact.normal);
        else if (receiver is StormInteractable wood) wood.Chop(contact.point, contact.normal);
        else if (receiver is StormWolf wolf) wolf.Strike(contact.point);
    }

    private Component FindTarget(Ray ray, out RaycastHit contact, out RaycastHit directHit)
    {
        int layers = Physics.DefaultRaycastLayers;
        const QueryTriggerInteraction triggers = QueryTriggerInteraction.Ignore;
        if (Physics.Raycast(ray, out directHit, HarvestReach, layers, triggers))
        {
            var directTarget = TargetOn(directHit.collider, directHit.distance);
            if (directTarget != null)
            {
                contact = directHit;
                return directTarget;
            }
        }

        // Allow some aim tolerance on shallow or narrow objects. A second ray must
        // reach the same object, so nearby targets cannot be used through walls.
        int count = Physics.SphereCastNonAlloc(ray, AimRadius, aimHits, HarvestReach, layers, triggers);
        Component closestTarget = null;
        float bestScore = float.PositiveInfinity;
        contact = default;
        for (int i = 0; i < count; i++)
        {
            var candidate = aimHits[i];
            Vector3 point = Vector3.MoveTowards(candidate.point, candidate.collider.bounds.center, .025f);
            Vector3 offset = point - ray.origin;
            float forward = Vector3.Dot(offset, ray.direction);
            if (forward <= 0f) continue;
            float distance = offset.magnitude;
            float score = 1f - forward / distance + distance * .002f;
            if (score >= bestScore) continue;

            var candidateTarget = TargetOn(candidate.collider, distance - .025f);
            if (candidateTarget == null ||
                !Physics.Raycast(ray.origin, offset / distance, out var sight, distance + .03f, layers, triggers) ||
                TargetOn(sight.collider, sight.distance) != candidateTarget) continue;

            closestTarget = candidateTarget;
            contact = sight;
            bestScore = score;
        }
        return closestTarget;
    }

    private static Component TargetOn(Collider collider, float distance)
    {
        var adventure = collider.GetComponentInParent<StormInteractable>();
        if (adventure != null) return distance <= HarvestReach && adventure.Available ? adventure : null;
        var mine = collider.GetComponentInParent<Mineable>();
        if (mine != null) return distance <= HarvestReach && mine.CanMine ? mine : null;
        if (distance > UseReach) return null;

        var wolf = collider.GetComponentInParent<StormWolf>();
        if (wolf != null) return wolf.CanTarget ? wolf : null;
        var sled = collider.GetComponentInParent<StormSupplySled>();
        if (sled != null) return sled.CanTarget ? sled : null;
        var searchlight = collider.GetComponentInParent<StormSearchlight>();
        if (searchlight != null) return searchlight;

        var food = collider.GetComponentInParent<Eatable>();
        if (food != null && food.FoodAmount > 1) return food;
        return (Component)collider.GetComponentInParent<WaterDrinkable>()
            ?? (Component)collider.GetComponentInParent<Generator>()
            ?? (Component)collider.GetComponentInParent<OilBarrel>()
            ?? (Component)collider.GetComponentInParent<Lantern>()
            ?? collider.GetComponentInParent<Spotlight>();
    }

    private void Tell(string message) { if (StormGame.Instance != null) StormGame.Instance.Toast(message, 5f); }
    public void CancelInteraction() { CurrentPrompt = string.Empty; pendingHarvest = null; CancelHold(); }
    private void CancelHold() { holding = null; HoldProgress = 0f; }
    public void ReleaseGrab()
    {
        if (heldSled != null) heldSled.EndPull();
        heldSled = null;
        if (joint != null) { joint.spring = 0f; joint.connectedBody = null; }
        grabbing = false;
    }
    private void Show(GameObject panel) { if (panel != null) panel.SetActive(true); }
    private void Value(Slider slider, float value, float maximum) { if (slider != null) { slider.maxValue = maximum; slider.value = value; } }
    private void HidePanels()
    {
        foreach (var panel in new[] { TakeOilPanel, refillText, LampInfo, EatPanel, DrinkPanel, GrabIcon, GeneratorPanel, RefillPanelGenerator, SpotlightPanel })
            if (panel != null) panel.SetActive(false);
    }
}
