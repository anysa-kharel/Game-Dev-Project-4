using System;

namespace StormSurvival
{
    // Engine-independent rules: inventory transactions and survival needs live here.
    [Serializable]
    public sealed class SurvivalState
    {
        public float Health = 80f, Warmth = 55f, Wetness = 30f, Stamina = 100f;
        public int Wood, Stone, Cloth;
        public bool HasPickaxe, HasTorch, TorchLit, FirstFire, BridgeBuilt, Rescued;
        public float TorchFuel;
        public bool Alive => Health > 0f;

        public bool SpendWood(int amount)
        {
            if (amount < 0 || Wood < amount) return false;
            Wood -= amount; return true;
        }

        public bool BuildCampfire()
        {
            if (Wood < 3 || Stone < 2) return false;
            Wood -= 3; Stone -= 2; FirstFire = true; return true;
        }

        public bool CraftTorch()
        {
            if (HasTorch)
            {
                if (TorchFuel >= 239f || !SpendWood(1)) return false;
                TorchFuel = Math.Min(240f, TorchFuel + 120f); return true;
            }
            if (Wood < 2 || Cloth < 1) return false;
            Wood -= 2; Cloth--; HasTorch = true; TorchFuel = 180f; return true;
        }

        public void Step(float seconds, bool sheltered, bool nearFire, float storm, bool torchHeld = true)
        {
            if (seconds <= 0f || !Alive || Rescued) return;
            storm = Math.Max(0f, Math.Min(1f, storm));
            if (TorchLit && torchHeld)
            {
                TorchFuel = Math.Max(0f, TorchFuel - seconds);
                if (TorchFuel <= 0f) TorchLit = false;
            }
            Wetness = Clamp(Wetness + seconds * (nearFire ? -7f : sheltered ? -2f : 0.35f + storm * 0.65f));
            float warmthChange = nearFire ? 10f : sheltered ? 0.7f :
                -(0.26f + storm * 0.40f + Wetness * 0.003f) * (TorchLit && torchHeld ? 0.55f : 1f);
            Warmth = Clamp(Warmth + seconds * warmthChange);
            if (Warmth <= 0f) Health = Clamp(Health - seconds * 3f);
        }

        public void Recover()
        {
            Health = 100f; Warmth = 90f; Wetness = 0f; Stamina = 100f;
            Wood = Math.Max(Wood, 3); Stone = Math.Max(Stone, 2);
            if (HasTorch) TorchFuel = Math.Max(TorchFuel, 120f);
        }

        public void Damage(float amount) { Health = Clamp(Health - Math.Max(0, amount)); }
        public static float Clamp(float value) => Math.Max(0f, Math.Min(100f, value));
    }
}
