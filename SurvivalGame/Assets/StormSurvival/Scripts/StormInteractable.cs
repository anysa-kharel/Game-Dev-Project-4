using System.Collections.Generic;
using UnityEngine;

namespace StormSurvival
{
    public sealed class StormInteractable : MonoBehaviour
    {
        public enum Kind { Wood, Campfire, Cache, Beacon }
        public static readonly List<StormInteractable> All = new List<StormInteractable>();
        public Kind kind;
        public string label;
        public int quantity = 4;
        public GameObject model, flame;
        public float fuel;
        private int strikes;
        private bool collected;
        private float regrow;
        public bool Available => !collected;
        public bool IsChoppable => kind == Kind.Wood;
        public string Prompt
        {
            get
            {
                var game = StormGame.Instance;
                var s = game.Needs;
                if (kind == Kind.Wood)
                    return !s.HasPickaxe ? "Select the pickaxe with 3" : "Left click  /  break down timber  " + strikes + "/3";
                if (kind == Kind.Campfire)
                {
                    if (fuel <= 0f) return "Hold E  /  build campfire  [3 wood + 2 stone]";
                    if (s.HasTorch && !s.TorchLit && s.TorchFuel > 0f) return "Hold E  /  light your torch";
                    if (fuel < 55f) return "Hold E  /  add fuel  [1 wood]";
                    return "Hold E  /  rest and save checkpoint";
                }
                if (kind == Kind.Beacon)
                {
                    if (game.World.Searchlight != null && !game.World.Searchlight.Aligned)
                        return "Align the lookout spotlight before lighting the beacon";
                    return "Hold E  /  light beacon  [6 wood + lit torch + power]";
                }
                if (kind == Kind.Cache) return "Hold E  /  take logging supplies";
                return string.Empty;
            }
        }
        public float HoldSeconds => kind == Kind.Beacon ? 2.5f : kind == Kind.Campfire ? 1.2f : 0.5f;
        private void OnEnable() { All.Add(this); }
        private void OnDisable() { All.Remove(this); }
        private void Update()
        {
            var game = StormGame.Instance;
            if (game == null || !game.Running) return;
            if (kind == Kind.Campfire)
            {
                fuel = Mathf.Max(0f, fuel - Time.deltaTime);
                if (flame != null) flame.SetActive(fuel > 0f);
            }
            if (kind == Kind.Wood && collected)
            {
                regrow -= Time.deltaTime;
                if (regrow <= 0f) { collected = false; strikes = 0; if (model != null) model.SetActive(true); }
            }
        }
        public void Chop(Vector3 contact, Vector3 normal)
        {
            var game = StormGame.Instance;
            if (game == null || !Available || !game.Needs.HasPickaxe || !IsChoppable) return;

            strikes++;
            bool breaks = strikes >= 3;
            StormHarvestEffects.Play(game, model, HarvestSurface.Wood, contact, normal, breaks);
            if (!breaks) return;

            collected = true;
            regrow = 65f;
            game.AddWood(quantity);
        }
        public void Use()
        {
            var game = StormGame.Instance; var s = game.Needs;
            if (!Available) return;
            switch (kind)
            {
                case Kind.Cache:
                    s.Cloth += 2; game.AddWood(3); collected = true;
                    if (model != null) model.SetActive(false);
                    game.Toast("Logging supplies: 3 wood, 2 cloth.", 6f); break;
                case Kind.Campfire:
                    if (fuel <= 0f)
                    {
                        if (!s.BuildCampfire()) { game.Toast("Need 3 wood + 2 stone. Gather timber and mine marked rocks with 3 + left click."); break; }
                        fuel = 150f; s.FirstFire = true; flame.SetActive(true);
                        game.Audio.Play("fire", transform.position, 0.65f); game.SetCheckpoint(this);
                    }
                    else if (s.HasTorch && !s.TorchLit && s.TorchFuel > 0f)
                    { s.TorchLit = true; game.Player.Inventory.selected = 0; game.Player.Inventory.Refresh(); game.Audio.Play("fire", transform.position, 0.4f); game.Toast("Torch lit. It slows heat loss. Follow the trail north.", 6f); }
                    else if (fuel < 55f)
                    {
                        if (s.SpendWood(1)) { fuel += 90f; game.Toast("Added fuel. Campfire burning for " + Mathf.CeilToInt(fuel) + " seconds."); }
                        else game.Toast("Need 1 wood to add fuel.");
                    }
                    else
                    { s.Warmth = Mathf.Max(s.Warmth, 85f); s.Wetness = Mathf.Max(0, s.Wetness - 50); game.SetCheckpoint(this); }
                    break;
                case Kind.Beacon:
                    if (!s.BridgeBuilt) { game.Toast("Reach the lookout by opening the river crossing first."); break; }
                    if (!s.TorchLit) { game.Toast("Light your torch at a campfire first. Rewrap an empty torch in the crafting menu."); break; }
                    if (game.RescueGenerator == null || !game.RescueGenerator.Toggled || game.RescueGenerator.CurrentFuel <= 0) { game.Toast("Refill and start the lookout generator first. Use 2 + R to refill, then E to start it.", 7f); break; }
                    if (game.World.Searchlight != null && !game.World.Searchlight.Aligned)
                    {
                        game.Toast("Rotate the powered spotlight toward the marked rescue clearing first.");
                        break;
                    }
                    if (!s.SpendWood(6)) { game.Toast("The beacon needs 6 wood. A marked woodpile is beside the lookout.", 6f); break; }
                    collected = true;
                    if (flame != null) flame.SetActive(true);
                    game.Win(); break;
            }
        }
    }
}
