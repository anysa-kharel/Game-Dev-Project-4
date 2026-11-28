using UnityEngine;
using TaskKind = StormSurvival.StormTrailTask.Kind;

namespace StormSurvival
{
    public sealed class StormBackcountry : MonoBehaviour
    {
        public const float CrossingNearEdge = 303.2f;
        public const float CrossingFarEdge = 321.8f;
        private const float RaisedAngle = -62f;
        private const float LoweringTime = 2.2f;
        private const float SouthShore = 303f;

        private StormGame game;
        private StormTrailTask handle, winch, westBreaker, eastBreaker, relay;
        private Transform bridge, winchCrank;
        private BoxCollider floor, leftRail, rightRail;
        private float lowerProgress;
        private bool lowering, westReset, eastReset;

        public bool HasWinchHandle { get; private set; }
        public bool CrossingReady { get; private set; }
        public bool RelayReady { get; private set; }
        public int ResetBreakers => (westReset ? 1 : 0) + (eastReset ? 1 : 0);

        public string Objective
        {
            get
            {
                if (!HasWinchHandle) return "Find the winch handle in the ranger shed.";
                if (lowering) return "Wait for the washout bridge to settle.";
                if (!CrossingReady) return "Press E at the service winch to lower the bridge.";
                if (BeforeCrossing) return "Cross the lowered footbridge.";
                if (ResetBreakers < 2) return $"Reset the trail breakers. {ResetBreakers}/2 online.";
                return "Press E at the rescue relay to restore the signal.";
            }
        }

        public Vector3 ObjectivePosition
        {
            get
            {
                if (!HasWinchHandle) return handle.transform.position;
                if (!CrossingReady) return winch.transform.position;
                if (BeforeCrossing) return Ground(0f, CrossingFarEdge + 2.4f);
                if (westReset && eastReset) return relay.transform.position;
                if (westReset) return eastBreaker.transform.position;
                if (eastReset) return westBreaker.transform.position;
                Vector3 player = game.Player.transform.position;
                return (player - westBreaker.transform.position).sqrMagnitude <= (player - eastBreaker.transform.position).sqrMagnitude
                    ? westBreaker.transform.position : eastBreaker.transform.position;
            }
        }

        private bool BeforeCrossing => game.Player.transform.position.z < CrossingFarEdge + .2f;

        public static StormBackcountry Build(StormGame game, StormWorld world, Transform parent, bool isPreview)
        {
            var root = Group("Washout crossing and rescue relay", parent, Vector3.zero);
            var route = root.gameObject.AddComponent<StormBackcountry>();
            route.game = game;
            var casing = world.MakeMaterial(new Color(.22f, .27f, .25f));
            var grip = world.MakeMaterial(new Color(.68f, .32f, .13f));
            var online = world.MakeMaterial(new Color(.37f, .81f, .56f), true);

            route.handle = BuildHandle(world, root, casing, grip);
            route.winch = route.BuildWinch(world, root, casing, grip, online);
            route.westBreaker = route.BuildControl(world, root, "West trail breaker", TaskKind.WestBreaker,
                Ground(-19f, 351.5f), casing, grip, online);
            route.eastBreaker = route.BuildControl(world, root, "East trail breaker", TaskKind.EastBreaker,
                Ground(20f, 388f), casing, grip, online);
            route.relay = route.BuildControl(world, root, "Rescue relay controls", TaskKind.RescueRelay,
                Ground(-4f, 407.5f), casing, grip, online);
            route.handle.Initialize(route, TaskKind.WinchHandle, null, null);
            route.BuildBridge(world, root, casing, isPreview);
            return route;
        }

        internal string PromptFor(TaskKind kind)
        {
            if (game.World.LookoutGate is { IsOpen: false }) return "Open the counterweight gate first";
            switch (kind)
            {
                case TaskKind.WinchHandle:
                    return "E / Take winch handle";
                case TaskKind.ServiceWinch:
                    if (CrossingReady) return "Washout bridge lowered";
                    if (lowering) return "Lowering the washout bridge";
                    return HasWinchHandle ? "E / Fit handle and lower bridge" : "Find the handle in the ranger shed";
                case TaskKind.WestBreaker:
                    if (!CrossingReady) return "Lower the washout bridge first";
                    return westReset ? "West breaker online" : "E / Reset west breaker";
                case TaskKind.EastBreaker:
                    if (!CrossingReady) return "Lower the washout bridge first";
                    return eastReset ? "East breaker online" : "E / Reset east breaker";
                case TaskKind.RescueRelay:
                    if (!CrossingReady) return "Lower the washout bridge first";
                    if (RelayReady) return "Rescue relay online";
                    return ResetBreakers == 2 ? "E / Restore rescue relay" : $"Reset both breakers   {ResetBreakers}/2";
                default:
                    return string.Empty;
            }
        }

        internal void Use(StormTrailTask task)
        {
            if (!game.Running) return;
            if (game.World.LookoutGate is { IsOpen: false } || (!CrossingReady &&
                task.TaskKind is TaskKind.WestBreaker or TaskKind.EastBreaker or TaskKind.RescueRelay))
            {
                game.Toast(task.Prompt);
                return;
            }

            switch (task.TaskKind)
            {
                case TaskKind.WinchHandle:
                    HasWinchHandle = true;
                    game.Audio.Play("collect", task.transform.position, .55f);
                    task.gameObject.SetActive(false);
                    game.Toast("Winch handle found. Follow the trail to the washout.", 5f);
                    break;
                case TaskKind.ServiceWinch:
                    if (lowering || CrossingReady) break;
                    if (!HasWinchHandle) { game.Toast(task.Prompt); break; }
                    lowering = true;
                    winchCrank.gameObject.SetActive(true);
                    game.Audio.Play("gate lift", task.transform.position, .72f);
                    game.Toast("Lowering the bridge. Wait for it to settle.", 4f);
                    break;
                case TaskKind.WestBreaker:
                    if (westReset) break;
                    westReset = true;
                    FinishBreaker(task);
                    break;
                case TaskKind.EastBreaker:
                    if (eastReset) break;
                    eastReset = true;
                    FinishBreaker(task);
                    break;
                case TaskKind.RescueRelay:
                    if (RelayReady) break;
                    if (ResetBreakers < 2) { game.Toast(task.Prompt); break; }
                    RelayReady = true;
                    task.ShowCompleted();
                    game.Audio.Play("signal reply", task.transform.position, .62f);
                    game.Toast("Rescue relay online. Follow the summit trail to the beacon.", 6f);
                    break;
            }
        }

        private void FinishBreaker(StormTrailTask task)
        {
            task.ShowCompleted();
            game.Audio.Play("spotlight turn", task.transform.position, .65f);
            game.Toast(ResetBreakers == 2 ? "Both breakers online. Head to the rescue relay." : "One breaker online. Find the other trail breaker.", 5f);
        }

        private void Update()
        {
            if (!lowering || !game.Running) return;
            lowerProgress = Mathf.MoveTowards(lowerProgress, 1f, Time.deltaTime / LoweringTime);
            float progress = Mathf.SmoothStep(0f, 1f, lowerProgress);
            bridge.localRotation = Quaternion.Euler(Mathf.Lerp(RaisedAngle, 0f, progress), 0f, 0f);
            winchCrank.localRotation = Quaternion.Euler(0f, 0f, progress * 540f);
            if (lowerProgress < 1f) return;

            floor.enabled = leftRail.enabled = rightRail.enabled = true;
            Physics.SyncTransforms();
            lowering = false;
            CrossingReady = true;
            winch.ShowCompleted();
            game.Toast("Bridge ready. Cross the washout to the relay trail.", 5f);
        }

        private static StormTrailTask BuildHandle(StormWorld world, Transform root, Material casing, Material grip)
        {
            var bench = Group("Ranger repair bench", root, Ground(-16f, 282f));
            world.Shape("Timber bench top", PrimitiveType.Cube, bench, new Vector3(0f, .91f, 0f), new Vector3(1.58f, .12f, .74f), world.Wood);
            for (int side = -1; side <= 1; side += 2)
                world.Shape("Repair bench leg", PrimitiveType.Cube, bench, new Vector3(.61f * side, .45f, 0f), new Vector3(.13f, .90f, .53f), casing);

            var item = Group("Winch handle", bench, new Vector3(0f, 1.08f, 0f));
            var stem = world.Shape("Steel handle stem", PrimitiveType.Cylinder, item, Vector3.zero, new Vector3(.10f, .39f, .10f), world.Metal, false);
            stem.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            world.Shape("Orange handle grip", PrimitiveType.Cube, item, new Vector3(.29f, .01f, 0f), new Vector3(.45f, .15f, .18f), grip, false);
            world.Shape("Handle reflector", PrimitiveType.Cube, item, new Vector3(.29f, .01f, -.096f), new Vector3(.19f, .047f, .04f), world.Amber, false);
            var hit = item.gameObject.AddComponent<BoxCollider>();
            hit.size = new Vector3(1.08f, .27f, .66f);
            return item.gameObject.AddComponent<StormTrailTask>();
        }

        private StormTrailTask BuildWinch(StormWorld world, Transform root, Material casing, Material grip, Material online)
        {
            var stand = Group("Service winch", root, Ground(-3.6f, 301.3f));
            world.Shape("Winch pedestal", PrimitiveType.Cube, stand, new Vector3(0f, .44f, 0f), new Vector3(.91f, .97f, .76f), casing);
            var drum = world.Shape("Winch drum", PrimitiveType.Cylinder, stand, new Vector3(0f, 1.10f, 0f), new Vector3(.65f, .34f, .65f), world.Metal);
            drum.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            var lamp = StatusLamp(world, stand, new Vector3(-.29f, 1.08f, -.40f));
            winchCrank = Group("Crank", stand, new Vector3(0f, 1.10f, -.38f));
            world.Shape("Crank arm", PrimitiveType.Cube, winchCrank, new Vector3(.20f, 0f, -.05f), new Vector3(.52f, .07f, .09f), world.Metal, false);
            world.Shape("Crank grip", PrimitiveType.Cube, winchCrank, new Vector3(.40f, 0f, -.12f), new Vector3(.15f, .14f, .24f), grip, false);
            winchCrank.gameObject.SetActive(false);
            var task = stand.gameObject.AddComponent<StormTrailTask>();
            task.Initialize(this, TaskKind.ServiceWinch, lamp, online);
            return task;
        }

        private StormTrailTask BuildControl(StormWorld world, Transform root, string name, TaskKind kind, Vector3 position,
            Material casing, Material grip, Material online)
        {
            var stand = Group(name, root, position);
            world.Shape("Control post", PrimitiveType.Cube, stand, new Vector3(0f, .59f, 0f), new Vector3(.30f, 1.30f, .31f), world.Wood);
            world.Shape("Weatherproof switch box", PrimitiveType.Cube, stand, new Vector3(0f, 1.15f, 0f), new Vector3(.95f, .65f, .48f), casing);
            world.Shape("Switch box brow", PrimitiveType.Cube, stand, new Vector3(0f, 1.50f, -.04f), new Vector3(1.07f, .11f, .63f), world.Metal, false);
            world.Shape("Orange breaker lever", PrimitiveType.Cube, stand, new Vector3(-.18f, 1.10f, -.27f), new Vector3(.19f, .22f, .10f), grip, false);
            var lamp = StatusLamp(world, stand, new Vector3(.23f, 1.20f, -.25f));
            if (kind == TaskKind.RescueRelay)
            {
                world.Shape("Relay antenna", PrimitiveType.Cylinder, stand, new Vector3(0f, 1.85f, .09f), new Vector3(.08f, .36f, .08f), world.Metal, false);
                world.Shape("Relay antenna cap", PrimitiveType.Sphere, stand, new Vector3(0f, 2.25f, .09f), new Vector3(.16f, .16f, .16f), world.Amber, false);
            }
            var task = stand.gameObject.AddComponent<StormTrailTask>();
            task.Initialize(this, kind, lamp, online);
            return task;
        }

        private static Renderer StatusLamp(StormWorld world, Transform stand, Vector3 position) =>
            world.Shape("Amber status lamp", PrimitiveType.Cube, stand, position, new Vector3(.16f, .11f, .07f), world.Amber, false).GetComponent<Renderer>();

        private void BuildBridge(StormWorld world, Transform root, Material casing, bool isPreview)
        {
            float length = CrossingFarEdge - CrossingNearEdge;
            float hingeHeight = StormWorld.Height(SouthShore) - .125f;
            bridge = Group("Washout footbridge", root, new Vector3(0f, hingeHeight, CrossingNearEdge));
            bridge.localRotation = Quaternion.Euler(RaisedAngle, 0f, 0f);
            var deck = new Vector3(0f, .05f, length * .5f);
            world.Shape("Bridge deck underlay", PrimitiveType.Cube, bridge, deck, new Vector3(3.10f, .24f, length), world.Wood, false);
            int planks = Mathf.CeilToInt(length / .58f);
            for (int index = 0; index < planks; index++)
            {
                float z = (index + .5f) * length / planks;
                world.Shape("Bridge plank", PrimitiveType.Cube, bridge, new Vector3(0f, .145f, z),
                    new Vector3(3.08f, .10f, length / planks - .024f), world.Wood, false);
            }

            for (int side = -1; side <= 1; side += 2)
            {
                float x = 1.62f * side;
                world.Shape("Handrail", PrimitiveType.Cube, bridge, new Vector3(x, .88f, length * .5f), new Vector3(.12f, .13f, length), casing, false);
                for (int post = 0; post < 7; post++)
                {
                    float z = .35f + post * ((length - .70f) / 6f);
                    world.Shape("Bridge rail post", PrimitiveType.Cube, bridge, new Vector3(x, .52f, z), new Vector3(.15f, 1.03f, .15f), world.Wood, false);
                }
            }

            for (int side = -1; side <= 1; side += 2)
                world.Shape("Bridge hinge footing", PrimitiveType.Cube, root, Ground(1.83f * side, CrossingNearEdge) + Vector3.up * .19f,
                    new Vector3(.69f, .59f, .87f), casing);

            if (isPreview) return;
            floor = DisabledCollider(bridge, new Vector3(0f, .045f, length * .5f), new Vector3(3.15f, .30f, length));
            leftRail = DisabledCollider(bridge, new Vector3(-1.63f, .57f, length * .5f), new Vector3(.18f, .84f, length));
            rightRail = DisabledCollider(bridge, new Vector3(1.63f, .57f, length * .5f), new Vector3(.18f, .84f, length));
        }

        private static BoxCollider DisabledCollider(Transform parent, Vector3 center, Vector3 size)
        {
            var collider = parent.gameObject.AddComponent<BoxCollider>();
            collider.enabled = false;
            collider.center = center;
            collider.size = size;
            return collider;
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }

        private static Vector3 Ground(float x, float z) => new Vector3(x, StormWorld.Height(z), z);
    }
}
