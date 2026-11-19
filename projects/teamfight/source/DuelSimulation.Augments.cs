using System;

public sealed partial class DuelSimulation
{
    public bool AugmentsEnabled { get; set; }
    public bool SelectingAugment { get; private set; }
    public AugmentTier CurrentAugmentTier { get; private set; }
    public float AugmentSeconds { get; private set; }
    // Offers and picks remain available after the round advances so clients can acknowledge the final choice.
    public int AugmentChoiceRound { get; private set; }
    public const float RoundOverDuration = 1;
    public float RoundOverSeconds { get; private set; }
    bool resultAfterRoundOver;
    int roundWinner;
    public readonly float[] RoundEnergy = { AdventureRoundRules.Energy, AdventureRoundRules.Energy };
    readonly int[] nextRoundEnergy = new int[2];
    readonly float[] towerAttackRate = { 1, 1 };
    readonly int[][] augmentOffers = new int[2][];
    readonly int[] augmentPicks = { -1, -1 };
    readonly Random augmentRandom = new Random();
    Definition[][] originalDefinitions;
    int[][] healthDamageStacks;
    public int AugmentOffer(int team, int slot) => augmentOffers[team] == null ? -1 : augmentOffers[team][slot];
    public int AugmentPick(int team) => augmentPicks[team];

    void BeginRoundOver(bool matchEnded)
    {
        RoundOverSeconds = RoundOverDuration;
        resultAfterRoundOver = matchEnded;
        int score = RoundScore(); roundWinner = score == 0 ? -1 : score > 0 ? 0 : 1;
    }
    void TickRoundOver(float dt)
    {
        RoundOverSeconds = Math.Max(0, RoundOverSeconds - dt);
        if (RoundOverSeconds > .0001f) return;
        RoundOverSeconds = 0;
        if (resultAfterRoundOver) Finish(roundWinner);
        else BeginAugments();
    }

    void BeginAugments()
    {
        AugmentChoiceRound = Rounds.Round;
        CurrentAugmentTier = BattleAugmentRules.RollTier(augmentRandom);
        for (int team = 0; team < 2; team++)
        {
            var pool = decks[team];
            if (pool == null)
            {
                pool = new int[definitions.Length];
                for (int kind = 0; kind < pool.Length; kind++) pool[kind] = kind;
            }
            augmentOffers[team] = BattleAugmentRules.Offer(CurrentAugmentTier, pool, augmentRandom);
            augmentPicks[team] = -1;
        }
        SelectingAugment = true; AugmentSeconds = BattleAugmentRules.SelectionSeconds;
    }
    public bool SelectAugment(int team, int slot, int round)
    {
        if (Finished || !SelectingAugment || round != AugmentChoiceRound || round != Rounds.Round || team < 0 || team > 1
            || slot < 0 || slot > 2 || augmentPicks[team] >= 0) return false;
        int code = augmentOffers[team][slot];
        augmentPicks[team] = code; ApplyAugment(team, code);
        if (augmentPicks[0] >= 0 && augmentPicks[1] >= 0)
        {
            SelectingAugment = false; AugmentSeconds = 0;
            if (Rounds.StartNextRound() == AdventureRoundRules.Change.RoundStarted) ResetRoundBudget();
        }
        return true;
    }
    void TickAugments(float dt)
    {
        AugmentSeconds = Math.Max(0, AugmentSeconds - dt);
        if (AugmentSeconds > 0) return;
        int choiceRound = AugmentChoiceRound;
        for (int team = 0; team < 2 && SelectingAugment; team++)
            if (augmentPicks[team] < 0) SelectAugment(team, augmentRandom.Next(3), choiceRound);
    }
    void ApplyAugment(int team, int code)
    {
        var effect = BattleAugmentRules.Effect(code);
        if (effect == AugmentEffect.TowerAttackSpeed) { towerAttackRate[team] *= 1.2f; return; }
        if (effect == AugmentEffect.NextRoundEnergy) { nextRoundEnergy[team] += 10; return; }
        if (originalDefinitions == null)
        {
            originalDefinitions = new Definition[2][]; healthDamageStacks = new int[2][];
            for (int side = 0; side < 2; side++)
            {
                if (teamDefinitions[side] == null) teamDefinitions[side] = Array.ConvertAll(definitions, entry => entry.Copy());
                originalDefinitions[side] = Array.ConvertAll(teamDefinitions[side], entry => entry.Copy());
                healthDamageStacks[side] = new int[definitions.Length];
            }
        }
        int kind = BattleAugmentRules.Kind(code);
        var definition = DefinitionFor(team, kind);
        switch (effect)
        {
            case AugmentEffect.AttackSpeed: definition.AttackRate *= 1.2f; break;
            case AugmentEffect.HealthAndSize: definition.Health *= 1.2f; definition.Scale *= 1.3f; break;
            case AugmentEffect.MovementSpeed: definition.Speed *= 1.1f; break;
            case AugmentEffect.HealthDamage: healthDamageStacks[team][kind]++; break;
            case AugmentEffect.TotalAttackSpeed: definition.AttackRate *= 1.5f; break;
            case AugmentEffect.Range: definition.Range *= 1.2f; break;
            case AugmentEffect.Cost: definition.Cost = Math.Max(1, definition.Cost - 1); break;
            case AugmentEffect.Twins: definition.Copies *= 2; definition.Health *= .5f; break;
            case AugmentEffect.StationaryTaunt: definition.Stationary = true; break;
            case AugmentEffect.Unlimited: definition.Unlimited = true; break;
        }
        definition.Damage = originalDefinitions[team][kind].Damage + definition.Health * .03f * healthDamageStacks[team][kind] * relicBonuses[team].Damage;
    }
}
