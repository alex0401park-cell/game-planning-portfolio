using System;
using System.Collections.Generic;

public enum BattleSpell { Fireball, Lightning, Frost, Poison }
public static class BattleTempo
{
    public const float Movement = .80f;
    public const float Attack = .80f;
}
public static class BattleSpellRules
{
    // Shared ground projection for targeting, visual footprint and authoritative hits.
    public const float GroundAspect = .45f;
    public const int Count = 4;
    public const int Capacity = 16;
    public static bool Valid(int kind) => kind >= 0 && kind < Count;
    public static int Cost(int kind) => kind == 0 ? 4 : kind == 1 ? 2 : kind == 2 ? 3 : 4;
    public static string Name(int kind) => new[] { "화염구", "번개", "빙결", "독" }[kind];
    public static float Radius(int kind) => kind == 1 ? 1.4f : kind == 0 ? 1.8f : 2f;
    public static float Delay(int kind) => kind == 0 ? 1.65f : kind == 1 ? .76f : .4f;
    public static float Duration(int kind) => kind == 3 ? 5f : kind == 2 ? 2f : kind == 1 ? 3f : 0;
    public static float ActiveDuration(SpellSnapshot spell) => Duration(spell.Kind) + spell.ExtraDuration;
    public static float Damage(int kind) => kind == 0 ? 160f : kind == 1 ? 15f : kind == 3 ? 18f : 0;
    public static float Cooldown(int kind) => 0f;
    public static bool ValidPoint(float x,float y) => !float.IsNaN(x)&&!float.IsNaN(y)&&!float.IsInfinity(x)&&!float.IsInfinity(y)&&Math.Abs(x)<=12&&Math.Abs(y)<=5;
    public static bool Contains(SpellSnapshot spell,float x,float y)
    { float dx=x-spell.X,dy=(y-spell.Y)/GroundAspect,r=Radius(spell.Kind);return dx*dx+dy*dy<=r*r; }
    public static bool Intersects(SpellSnapshot spell,ArenaNavigation.Obstacle bounds)
    {return Contains(spell,Math.Max(bounds.MinX,Math.Min(bounds.MaxX,spell.X)),Math.Max(bounds.MinY,Math.Min(bounds.MaxY,spell.Y)));}

}
public struct SpellSnapshot
#if TEAMFIGHT_FUSION
    : Fusion.INetworkStruct
#endif
{
    public int Id, Team, Kind;
    public float X, Y, Age, ExtraDuration, EmberDps;
}
/// <summary>Shared deterministic timing, capacity and cooldown rules for local and server battles.</summary>
public sealed class BattleSpellTimeline
{
    public readonly List<SpellSnapshot> Active = new List<SpellSnapshot>();
    readonly float[,] ready = new float[2,4];
    float clock;
    int serial;
    public float ReadyIn(int team,int kind) => Math.Max(0,ready[team,kind]-clock);
    public bool TryCast(int team,int kind,float x,float y,float energy,out string reason)
    {
        reason="";
        if(team<0||team>1||!BattleSpellRules.Valid(kind)||!BattleSpellRules.ValidPoint(x,y)){reason="Invalid spell target.";return false;}
        if(Active.Count>=BattleSpellRules.Capacity){reason="Too many active spells.";return false;}
        if(ReadyIn(team,kind)>0){reason="Spell is cooling down.";return false;}
        if(energy<BattleSpellRules.Cost(kind)){reason="Not enough energy.";return false;}
        ready[team,kind]=clock+BattleSpellRules.Cooldown(kind);
        Active.Add(new SpellSnapshot{Id=++serial,Team=team,Kind=kind,X=x,Y=y});return true;
    }
    public bool BlocksHealing(int team,float x,float y)
    {
        foreach(var s in Active)if(s.Kind==3&&s.Team!=team&&s.Age>=BattleSpellRules.Delay(3)&&s.Age<BattleSpellRules.Delay(3)+BattleSpellRules.ActiveDuration(s)&&BattleSpellRules.Contains(s,x,y))return true;
        return false;
    }
    public void Tick(float dt,Action<SpellSnapshot,float> apply)
    {
        if(dt<=0||float.IsNaN(dt)||float.IsInfinity(dt))return;
        clock+=dt;
        for(int i=Active.Count-1;i>=0;i--){var s=Active[i];float previous=s.Age;s.Age+=dt;Active[i]=s;
            float delay=BattleSpellRules.Delay(s.Kind);
            if(s.Kind==3){float overlap=Math.Max(0,Math.Min(s.Age,delay+BattleSpellRules.ActiveDuration(s))-Math.Max(previous,delay));if(overlap>0)apply(s,overlap);}
            else {
                if(previous<delay&&s.Age>=delay)apply(s,1);
                if(s.Kind==0 && s.EmberDps>0) {
                    float overlap=Math.Max(0,Math.Min(s.Age,delay+s.ExtraDuration)-Math.Max(previous,delay));
                    if(overlap>0)apply(s,-overlap);
                }
            }
            if(s.Age>delay+Math.Max(1.2f,BattleSpellRules.ActiveDuration(s)))Active.RemoveAt(i);
        }
    }
    public void Clear(){Active.Clear();Array.Clear(ready,0,ready.Length);}
}
