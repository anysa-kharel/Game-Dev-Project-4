using System.Collections.Generic;
using UnityEngine;

namespace StormSurvival
{
    public sealed class StormLookoutGate : MonoBehaviour
    {
        public const int RequiredStones = 6;
        public const float GateZ = 248.5f;
        private const float HalfOpening = 2.4f;
        private const float LiftHeight = 3.55f;
        private const float LiftSeconds = 2.2f;
        private const float StandX = 4.3f;
        private const float StandZ = 244.3f;
        private static readonly Vector3 TrayOffset = new Vector3(-1.05f, -.32f, 0f);
        private static readonly Vector3 WinchAnchor = new Vector3(1.93f, 7.21f, -.62f);

        private readonly List<GameObject> weights = new List<GameObject>(RequiredStones);
        private StormGame game;
        private Transform panel, lever, tray, counterweight, hoistCable;
        private float angle = -13f;
        private float lift;

        public int DepositedStones { get; private set; }
        public int StonesRemaining => RequiredStones - DepositedStones;
        public bool IsOpen { get; private set; }
        public Vector3 ScalePosition => tray.position + Vector3.up * .13f;

        public string Prompt
        {
            get
            {
                if (IsOpen) return "Lookout gate open";
                if (StonesRemaining == 0) return "Lookout gate lifting";
                string progress = $"{DepositedStones}/{RequiredStones} stone";
                return game.Needs.Stone > 0 ? "E  /  Deposit stone  ·  " + progress : "Mine marked rocks  ·  " + progress;
            }
        }

        internal string Objective
        {
            get
            {
                if (StonesRemaining == 0) return "The lookout gate is lifting.";
                string progress = $"{DepositedStones}/{RequiredStones} stone deposited.";
                return game.Needs.Stone >= StonesRemaining
                    ? "Press E at the gate scale. " + progress
                    : "Mine marked rocks by the gate. " + progress;
            }
        }

        internal static StormLookoutGate Build(StormGame game, StormWorld world, Transform parent,
            float forestBoundary, Material stone)
        {
            var entrance = Group("Lookout gate entrance", parent, new Vector3(0f, StormWorld.Height(GateZ), GateZ));
            var iron = world.MakeMaterial(new Color(.21f, .25f, .25f));
            var edge = world.MakeMaterial(new Color(.43f, .46f, .42f));
            var leverBase = new Vector3(StandX, StormWorld.Height(StandZ) - entrance.position.y, StandZ - GateZ);
            var stand = Group("Lookout counterweight scale", entrance, leverBase);
            var gate = stand.gameObject.AddComponent<StormLookoutGate>();
            gate.game = game;
            gate.BuildBarrier(world, entrance, forestBoundary, stone, iron, edge);
            gate.BuildScale(world, stone, iron, edge);
            gate.PoseScale();
            return gate;
        }

        public void Use()
        {
            if (!game.Running || StonesRemaining == 0) return;

            int amount = Mathf.Min(StonesRemaining, game.Needs.Stone);
            if (amount <= 0)
            {
                game.Toast("Mine the amber-marked rocks on either side of the trail for stone.");
                return;
            }

            game.Needs.Stone -= amount;
            for (int index = DepositedStones; index < DepositedStones + amount; index++)
                weights[index].SetActive(true);
            DepositedStones += amount;
            game.Audio.PlayHarvest(HarvestSurface.Stone, false, ScalePosition);
            if (StonesRemaining > 0)
            {
                game.Toast($"Gate scale  ·  {DepositedStones}/{RequiredStones} stone");
                return;
            }

            game.Audio.Play("gate lift", panel.position, .8f);
            game.Toast("Scale balanced. The lookout gate is lifting.");
        }

        private void Update()
        {
            if (!game.Running || IsOpen) return;

            float desiredAngle = Mathf.Lerp(-13f, 15f, (float)DepositedStones / RequiredStones);
            if (angle != desiredAngle)
            {
                angle = Mathf.MoveTowards(angle, desiredAngle, Time.deltaTime * 36f);
                PoseScale();
            }
            if (StonesRemaining > 0) return;

            lift = Mathf.MoveTowards(lift, 1f, Time.deltaTime / LiftSeconds);
            panel.localPosition = Vector3.up * (LiftHeight * Mathf.SmoothStep(0f, 1f, lift));
            if (lift < 1f) return;

            IsOpen = true;
            game.Toast("Lookout gate open. Follow the trail to the generator.", 5f);
        }

        private void BuildBarrier(StormWorld world, Transform entrance, float forestBoundary,
            Material stone, Material iron, Material edge)
        {
            const int sections = 7;
            const float footing = -.60f;
            float sectionWidth = (forestBoundary + 1f - HalfOpening) / sections;
            for (int side = -1; side <= 1; side += 2)
            {
                for (int section = 0; section < sections; section++)
                {
                    float x = side * (HalfOpening + sectionWidth * (section + .5f));
                    float top = 5.05f + .36f * ((section + 1) % 3);
                    float z = .14f * (section % 3 - 1);
                    world.Shape("Stone retaining wall", PrimitiveType.Cube, entrance, new Vector3(x, (top + footing) * .5f, z),
                        new Vector3(sectionWidth + .20f, top - footing, 1.95f), stone);
                    world.Shape("Wall capstones", PrimitiveType.Cube, entrance, new Vector3(x, top + .06f, z),
                        new Vector3(sectionWidth + .25f, .28f, 2.15f), edge, false);
                }

                world.Shape("Gate timber upright", PrimitiveType.Cube, entrance, new Vector3(side * 2.60f, 3.20f, -.30f),
                    new Vector3(.46f, 7.35f, .86f), world.Wood);
                world.Shape("Iron gate track", PrimitiveType.Cube, entrance, new Vector3(side * 2.37f, 3.35f, -.09f),
                    new Vector3(.14f, 7.45f, .55f), iron, false);
                world.Shape("Reflective gate marker", PrimitiveType.Cube, entrance, new Vector3(side * 2.6f, 1.34f, -.75f),
                    new Vector3(.18f, .25f, .06f), world.Amber, false);
            }
            world.Shape("Gate winch crossbeam", PrimitiveType.Cube, entrance, new Vector3(0f, 7.21f, -.15f),
                new Vector3(5.85f, .46f, .68f), world.Wood);
            var drum = world.Shape("Counterweight winch", PrimitiveType.Cylinder, entrance, new Vector3(1.65f, 7.21f, -.58f),
                new Vector3(.66f, .53f, .66f), iron, false);
            drum.transform.localRotation = Quaternion.Euler(0f, 0f, 90f);
            hoistCable = world.Shape("Counterweight hoist cable", PrimitiveType.Cylinder, entrance,
                Vector3.zero, Vector3.one, iron, false).transform;

            panel = Group("Sliding lookout gate", entrance, Vector3.zero);
            var collider = panel.gameObject.AddComponent<BoxCollider>();
            collider.center = new Vector3(0f, 1.38f, 0f);
            collider.size = new Vector3(4.85f, 3.95f, .42f);
            for (int board = 0; board < 10; board++)
            {
                float x = (board - 4.5f) * .485f;
                world.Shape("Weathered gate plank", PrimitiveType.Cube, panel, new Vector3(x, 1.38f, board % 2 == 0 ? 0f : .025f),
                    new Vector3(.48f, 3.95f, .36f), world.Wood, false);
            }
            foreach (float y in new[] { .20f, 2.64f })
                world.Shape("Iron gate strap", PrimitiveType.Cube, panel, new Vector3(0f, y, -.224f),
                    new Vector3(4.88f, .19f, .08f), iron, false);
            var brace = world.Shape("Gate diagonal brace", PrimitiveType.Cube, panel, new Vector3(0f, 1.42f, -.232f),
                new Vector3(5.38f, .22f, .10f), world.Wood, false);
            brace.transform.localRotation = Quaternion.Euler(0f, 0f, 27f);
        }

        private void BuildScale(StormWorld world, Material stone, Material iron, Material edge)
        {
            world.Shape("Scale stone footing", PrimitiveType.Cube, transform, new Vector3(-.07f, .02f, 0f),
                new Vector3(2.82f, .38f, 1.32f), stone);
            world.Shape("Scale support post", PrimitiveType.Cube, transform, new Vector3(0f, .67f, 0f),
                new Vector3(.36f, 1.24f, .41f), world.Wood);
            var hub = world.Shape("Scale pivot", PrimitiveType.Cylinder, transform, new Vector3(0f, 1.29f, 0f),
                new Vector3(.49f, .22f, .49f), iron);
            hub.transform.localRotation = Quaternion.Euler(90f, 0f, 0f);
            lever = Group("Balance lever", transform, new Vector3(0f, 1.29f, 0f));
            world.Shape("Scale balance beam", PrimitiveType.Cube, lever, Vector3.zero, new Vector3(2.56f, .14f, .18f), iron);
            for (int sign = -1; sign <= 1; sign += 2)
                world.Shape("Scale suspension rod", PrimitiveType.Cylinder, lever, new Vector3(sign * 1.05f, -.14f, 0f),
                    new Vector3(.045f, .18f, .045f), edge, false);

            tray = Group("Stone loading tray", transform, Vector3.zero);
            world.Shape("Scale loading pan", PrimitiveType.Cube, tray, Vector3.zero, new Vector3(1.09f, .16f, .91f), iron);
            for (int sign = -1; sign <= 1; sign += 2)
            {
                world.Shape("Scale pan rim", PrimitiveType.Cube, tray, new Vector3(0f, .10f, sign * .44f),
                    new Vector3(1.09f, .13f, .06f), edge);
                world.Shape("Scale pan end", PrimitiveType.Cube, tray, new Vector3(sign * .52f, .10f, 0f),
                    new Vector3(.06f, .13f, .87f), edge);
            }
            world.Shape("Amber loading marker", PrimitiveType.Cube, tray, new Vector3(0f, .10f, -.48f),
                new Vector3(.48f, .055f, .035f), world.Amber, false);
            for (int index = 0; index < RequiredStones; index++)
            {
                float x = (index % 3 - 1) * .32f;
                float z = index < 3 ? -.16f : .15f;
                var weight = world.Shape("Deposited stone", PrimitiveType.Sphere, tray, new Vector3(x, .17f, z),
                    new Vector3(.30f, .21f, .25f), index % 2 == 0 ? stone : edge, false);
                weight.transform.localRotation = Quaternion.Euler(12f * index, index * 43f, 30f - index * 11f);
                weight.SetActive(false);
                weights.Add(weight);
            }
            counterweight = world.Shape("Scale counterweight", PrimitiveType.Cylinder, transform, Vector3.zero,
                new Vector3(.39f, .26f, .39f), stone).transform;
        }

        private void PoseScale()
        {
            lever.localRotation = Quaternion.Euler(0f, 0f, angle);
            tray.localPosition = lever.localPosition + lever.localRotation * TrayOffset;
            counterweight.localPosition = lever.localPosition + lever.localRotation * new Vector3(1.05f, -.42f, 0f);
            Vector3 cableEnd = transform.localPosition + counterweight.localPosition + Vector3.up * .24f;
            Vector3 cableSpan = cableEnd - WinchAnchor;
            hoistCable.localPosition = (WinchAnchor + cableEnd) * .5f;
            hoistCable.localRotation = Quaternion.FromToRotation(Vector3.up, cableSpan);
            hoistCable.localScale = new Vector3(.055f, cableSpan.magnitude * .5f, .055f);
        }

        private static Transform Group(string name, Transform parent, Vector3 position)
        {
            var group = new GameObject(name).transform;
            group.SetParent(parent, false);
            group.localPosition = position;
            return group;
        }
    }
}
