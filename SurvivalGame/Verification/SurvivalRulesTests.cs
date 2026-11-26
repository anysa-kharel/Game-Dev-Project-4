using System;
using StormSurvival;
public static class SurvivalRulesTests
{
    private static int checks;
    private static void Check(bool result, string name)
    { if (!result) throw new Exception("FAILED: " + name); checks++; }
    private static bool Near(float a,float b) => Math.Abs(a-b)<.001f;
    public static void Main()
    {
        var s=new SurvivalState {Wood=2,Stone=2};
        Check(!s.BuildCampfire()&&s.Wood==2&&s.Stone==2,"Missing wood: campfire transaction unchanged");
        s.Wood=3;s.Stone=1;
        Check(!s.BuildCampfire()&&s.Wood==3&&s.Stone==1,"Missing stone: campfire transaction unchanged");
        s.Stone=2;
        Check(s.BuildCampfire()&&s.FirstFire&&s.Wood==0&&s.Stone==0,"Campfire consumes exact costs");
        Check(!s.SpendWood(-2)&&s.Wood==0,"Negative spending rejected");
        Check(!s.CraftTorch()&&!s.HasTorch,"Cannot craft without supplies");
        s.Wood=2;s.Cloth=0;
        Check(!s.CraftTorch()&&s.Wood==2,"Missing cloth preserves wood");
        s.Cloth=1;
        Check(s.CraftTorch()&&s.HasTorch&&s.Wood==0&&s.Cloth==0&&Near(s.TorchFuel,180),"Torch consumes exact costs");
        s.Wood=1;s.TorchFuel=240;
        Check(!s.CraftTorch()&&s.Wood==1,"Full torch cannot waste wood");
        s.TorchFuel=200;s.TorchLit=true;
        Check(s.CraftTorch()&&Near(s.TorchFuel,240)&&s.Wood==0&&s.TorchLit,"Rewrap caps fuel and preserves lit state");
        s.Step(10,false,false,.5f,true);
        Check(Near(s.TorchFuel,230),"Held torch consumes fuel");
        s.Step(10,false,false,.5f,false);
        Check(Near(s.TorchFuel,230),"Stowed torch conserves fuel");
        s.TorchFuel=1;s.Step(2,false,false,.5f,true);
        Check(Near(s.TorchFuel,0)&&!s.TorchLit,"Exhausted torch extinguishes");
        s.Wetness=50;s.Warmth=10;s.Step(3,true,false,1);
        Check(Near(s.Wetness,44)&&s.Warmth>10,"Shelter dries and slowly warms");
        s.Wetness=50;s.Warmth=10;s.Step(3,false,true,1);
        Check(Near(s.Wetness,29)&&Near(s.Warmth,40),"Fire rapidly dries and warms");
        var exposed=new SurvivalState {Warmth=80,Wetness=0};
        var held=new SurvivalState {Warmth=80,Wetness=0,TorchLit=true,TorchFuel=180};
        var stowed=new SurvivalState {Warmth=80,Wetness=0,TorchLit=true,TorchFuel=180};
        exposed.Step(10,false,false,1);held.Step(10,false,false,1,true);stowed.Step(10,false,false,1,false);
        Check(held.Warmth>exposed.Warmth&&Near(stowed.Warmth,exposed.Warmth),"Only held torch reduces heat loss");
        s.Warmth=0;s.Health=10;s.Step(2,false,false,1);
        Check(Near(s.Health,4),"Freezing damages health");
        s.Step(3,false,false,1);Check(!s.Alive&&Near(s.Health,0),"Health cannot fall below zero");
        s.Wood=9;s.Stone=5;s.Cloth=3;s.BridgeBuilt=true;s.Recover();
        Check(Near(s.Health,100)&&Near(s.Warmth,90)&&Near(s.Wetness,0),"Retry restores needs");
        Check(s.Wood==9&&s.Stone==5&&s.Cloth==3&&s.BridgeBuilt,"Retry retains inventory and opened route");
        s.Wood=0;s.Stone=0;s.TorchFuel=0;s.Recover();
        Check(s.Wood>=3&&s.Stone>=2&&s.TorchFuel>=120,"Retry provides recovery supplies");
        s.Rescued=true;float warmth=s.Warmth,wet=s.Wetness,fuel=s.TorchFuel;s.Step(100,false,false,1);
        Check(Near(s.Warmth,warmth)&&Near(s.Wetness,wet)&&Near(s.TorchFuel,fuel),"Rescue stops survival decay");
        s.Rescued=false;s.Step(0,false,false,1);s.Step(-1,false,false,1);
        Check(Near(s.Warmth,warmth)&&Near(s.TorchFuel,fuel),"Zero or negative elapsed time ignored");
        s.Damage(-1);Check(Near(s.Health,100),"Negative damage cannot heal");
        s.Damage(1000);Check(!s.Alive&&Near(s.Health,0),"Large damage is clamped");
        Console.WriteLine("PASS: "+checks+" survival rule assertions.");
    }
}
