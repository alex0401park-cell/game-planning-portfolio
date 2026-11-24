using System;
using System.Collections.Generic;

public enum AugmentTier { Silver, Gold, Prismatic }
public enum AugmentEffect
{
    AttackSpeed, HealthAndSize, MovementSpeed, TowerAttackSpeed, NextRoundEnergy,
    HealthDamage, TotalAttackSpeed, Range, Cost, Twins, StationaryTaunt, Unlimited, FireEmbers, LightningMark, FrostShatter, PoisonZone
}

public static class BattleAugmentRules
{
    public const float SelectionSeconds = 30;
    public static AugmentTier RollTier(Random random)
    {
        int roll = random.Next(100);
        return roll < 40 ? AugmentTier.Silver : roll < 75 ? AugmentTier.Gold : AugmentTier.Prismatic;
    }
    public static AugmentTier Tier(AugmentEffect effect)
        => SpellKind(effect) >= 0 ? AugmentTier.Gold : effect <= AugmentEffect.MovementSpeed ? AugmentTier.Silver
        : effect <= AugmentEffect.Range ? AugmentTier.Gold : AugmentTier.Prismatic;
    public static bool TargetsUnit(AugmentEffect effect)
        => effect != AugmentEffect.TowerAttackSpeed && effect != AugmentEffect.NextRoundEnergy;
    public static int Encode(AugmentEffect effect, int kind) => (int)effect * 65536 + kind + 1;
    public static AugmentEffect Effect(int code) => (AugmentEffect)(code / 65536);
    public static int Kind(int code) => code % 65536 - 1;
    public static int SpellKind(AugmentEffect effect) => effect >= AugmentEffect.FireEmbers && effect <= AugmentEffect.PoisonZone ? (int)effect - (int)AugmentEffect.FireEmbers : -1;
    public static int[] Offer(AugmentTier tier, int[] deck, Random random, Func<int,int> spellKind = null, Func<AugmentEffect,bool> eligible = null)
    {
        var effects = new List<List<int>>();
        int combinations = 0;
        foreach (AugmentEffect effect in Enum.GetValues(typeof(AugmentEffect)))
        {
            if (Tier(effect) != tier || eligible != null && !eligible(effect)) continue;
            var pool = new List<int>();
            if (TargetsUnit(effect))
            {
                foreach (int kind in deck)
                {
                    int spell = spellKind == null ? -1 : spellKind(kind);
                    if (SpellKind(effect) >= 0 ? spell != SpellKind(effect) : spell >= 0) continue;
                    int code = Encode(effect, kind);
                    if (!pool.Contains(code)) pool.Add(code);
                }
            }
            else pool.Add(Encode(effect, -1));
            if (pool.Count > 0) { effects.Add(pool); combinations += pool.Count; }
        }
        if (combinations < 3) throw new ArgumentException("An augment offer requires at least three distinct combinations.");
        var result = new int[3];
        for (int slot = 0; slot < result.Length; slot++)
        {
            int effectIndex = random.Next(effects.Count);
            var pool = effects[effectIndex];
            int index = random.Next(pool.Count);
            result[slot] = pool[index]; pool.RemoveAt(index);
            if (pool.Count == 0) effects.RemoveAt(effectIndex);
        }
        return result;
    }
    public static string Description(int code, string unit)
    {
        switch (Effect(code))
        {
            case AugmentEffect.AttackSpeed: return unit + "의 공격속도가 20% 증가합니다.";
            case AugmentEffect.HealthAndSize: return unit + "의 체력이 20%, 크기가 30% 증가합니다.";
            case AugmentEffect.MovementSpeed: return unit + "의 이동속도가 10% 증가합니다.";
            case AugmentEffect.TowerAttackSpeed: return "타워의 공격속도가 20% 증가합니다.";
            case AugmentEffect.NextRoundEnergy: return "다음 라운드의 총 에너지가 10 추가됩니다.";
            case AugmentEffect.HealthDamage: return unit + "의 최대 체력의 3%만큼 공격력이 증가합니다.";
            case AugmentEffect.TotalAttackSpeed: return unit + "의 총 공격속도가 1.5배가 됩니다.";
            case AugmentEffect.Range: return unit + "의 사거리가 1.2배가 됩니다.";
            case AugmentEffect.Cost: return unit + "의 코스트가 1 감소합니다. (최소 1)";
            case AugmentEffect.Twins: return unit + "이 2배 생성되지만 각각의 체력은 50%가 됩니다.";
            case AugmentEffect.StationaryTaunt: return unit + "이 이동하지 않고, 주변 적의 어그로를 끕니다.";
            default: return unit + "의 배치 수 제한이 사라집니다.";
        }
    }
}
