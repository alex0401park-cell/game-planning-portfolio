using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
#if UNITY_EDITOR
using UnityEditor;
#endif

public static class AugmentBattleChecks
{
    static void Require(bool condition, string message) { if (!condition) throw new Exception(message); }
    static void Near(float actual, float expected, string message) => Require(Math.Abs(actual - expected) < .005f, message + ": " + actual + " != " + expected);
    static DuelSimulation Create()
    {
        var definitions = Enumerable.Range(0, 7).Select(index => new DuelSimulation.Definition
            { Health = 100, Damage = 10, Speed = 2, Range = 1, Interval = 1, Cost = 3, Cells = 1 }).ToArray();
        var towers = new[] {
            new DuelSimulation.Tower { Team = 0, Main = true, Health = 100000, Maximum = 100000, Bounds = new ArenaNavigation.Obstacle(-11, 1, -10, 3) },
            new DuelSimulation.Tower { Team = 1, Main = true, Health = 100000, Maximum = 100000, Bounds = new ArenaNavigation.Obstacle(10, 1, 11, 3) } };
        var simulation = new DuelSimulation(definitions, towers, new List<ArenaNavigation.Obstacle>()) { AugmentsEnabled = true };
        simulation.ConfigureDeck(0, Enumerable.Range(0, 7).ToArray()); simulation.ConfigureDeck(1, Enumerable.Range(0, 7).ToArray());
        return simulation;
    }
    static void Apply(DuelSimulation simulation, int team, AugmentEffect effect, int kind = 0)
        => typeof(DuelSimulation).GetMethod("ApplyAugment", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(simulation, new object[] { team, BattleAugmentRules.Encode(effect, BattleAugmentRules.TargetsUnit(effect) ? kind : -1) });
    static int SetOffer(DuelSimulation simulation, int team, int slot, AugmentEffect effect)
    {
        int code = BattleAugmentRules.Encode(effect, BattleAugmentRules.TargetsUnit(effect) ? 0 : -1);
        var offers = (int[][])typeof(DuelSimulation).GetField("augmentOffers", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation);
        offers[team][slot] = code; return code;
    }
    static void Advance(DuelSimulation simulation, int steps)
    { for (int step = 0; step < steps; step++) simulation.Tick(.2f); }
    static void EndRound(DuelSimulation simulation, bool finishAnnouncement = true)
    {
        for (int step = 0; step < 310 && !simulation.Rounds.BetweenRounds && !simulation.Finished; step++) simulation.Tick(.2f);
        if (finishAnnouncement) while (simulation.RoundOverSeconds > 0) simulation.Tick(.2f);
    }
    sealed class Roll : Random
    {
        readonly int value;
        public Roll(int value) { this.value = value; }
        public override int Next(int maximum) => value;
    }
#if AUGMENT_CHECKS
    public static void Main() { Run(); Console.WriteLine("PASS augment battle checks"); }
#endif
#if UNITY_EDITOR
    [MenuItem("Tools/Online Duel/Validate Augments")]
#endif
    public static void Run()
    {
        int[] tierCounts = new int[3];
        for (int roll = 0; roll < 100; roll++) tierCounts[(int)BattleAugmentRules.RollTier(new Roll(roll))]++;
        Require(tierCounts.SequenceEqual(new[] { 40, 35, 25 }), "Tier probabilities");
        var random = new Random(71); bool repeatedEffect = false, repeatedUnit = false;
        for (int sample = 0; sample < 1000; sample++)
        {
            var tier = (AugmentTier)(sample % 3);
            var offer = BattleAugmentRules.Offer(tier, Enumerable.Range(0, 7).ToArray(), random);
            Require(offer.Distinct().Count() == 3 && offer.All(code => BattleAugmentRules.Tier(BattleAugmentRules.Effect(code)) == tier), "Offer tier or duplicate combination");
            repeatedEffect |= offer.Select(BattleAugmentRules.Effect).Distinct().Count() < 3;
            repeatedUnit |= offer.Select(BattleAugmentRules.Kind).Where(kind => kind >= 0).Distinct().Count() < offer.Count(code => BattleAugmentRules.Kind(code) >= 0);
            Require(offer.All(code => BattleAugmentRules.Kind(code) >= -1 && BattleAugmentRules.Kind(code) < 7), "Offer outside deck");
        }
        Require(repeatedEffect && repeatedUnit, "Independent random effects and targets");
        var simulation = Create();
        Apply(simulation, 0, AugmentEffect.AttackSpeed); Apply(simulation, 0, AugmentEffect.TotalAttackSpeed);
        Near(simulation.DefinitionFor(0, 0).AttackRate, 1.8f, "Multiplicative attack speed");
        Apply(simulation, 0, AugmentEffect.HealthDamage); Apply(simulation, 0, AugmentEffect.HealthAndSize);
        Near(simulation.DefinitionFor(0, 0).Health, 120, "Health multiplier");
        Near(simulation.DefinitionFor(0, 0).Damage, 13.6f, "Damage follows augmented max HP");
        Near(simulation.DefinitionFor(0, 0).Scale, 1.3f, "Size multiplier");
        Apply(simulation, 0, AugmentEffect.HealthDamage); Near(simulation.DefinitionFor(0, 0).Damage, 17.2f, "Repeated health damage");
        Apply(simulation, 0, AugmentEffect.MovementSpeed); Near(simulation.DefinitionFor(0, 0).Speed, 2.2f, "Movement");
        Apply(simulation, 0, AugmentEffect.Range); Near(simulation.DefinitionFor(0, 0).Range, 1.2f, "Range");
        for (int repeat = 0; repeat < 8; repeat++) Apply(simulation, 0, AugmentEffect.Cost);
        Require(simulation.DefinitionFor(0, 0).Cost == 1, "Minimum cost");
        Near(simulation.DefinitionFor(1, 0).Health, 100, "Opponent isolation");
        Apply(simulation, 0, AugmentEffect.Twins);
        Near(simulation.DefinitionFor(0, 0).Health, 60, "Twin max HP");
        Require(simulation.Deploy(0, 0, -7, 2.5f, out _), "Twin deployment");
        var twins = simulation.Units.Where(unit => unit != null).ToArray();
        Require(twins.Length == 2 && twins[0].Deployment == twins[1].Deployment, "Twins consume one deployment");
        Near(simulation.Elixir[0], 24, "Twins cost once");
        Require(twins.All(unit => Math.Abs(unit.Health - 60) < .005f), "Both twins have half HP");
        Apply(simulation, 0, AugmentEffect.TowerAttackSpeed);
        var rates = (float[])typeof(DuelSimulation).GetField("towerAttackRate", BindingFlags.Instance | BindingFlags.NonPublic).GetValue(simulation);
        Near(rates[0], 1.2f, "Tower attack speed"); Near(rates[1], 1, "Enemy tower unchanged");

        var rounds = new AdventureRoundRules();
        rounds.Tick(.2f, false, true);
        Require(!rounds.BetweenRounds && rounds.Remaining > 0, "One player unable to summon does not end the round");
        Require(rounds.Tick(.2f, false, false) == AdventureRoundRules.Change.RoundEnded && rounds.Remaining > 0, "Both players unable to summon end the round early");
        rounds = new AdventureRoundRules();
        Require(rounds.Tick(AdventureRoundRules.Duration, true, true) == AdventureRoundRules.Change.RoundEnded && rounds.Remaining == 0, "Timer zero always ends the round");
        Near(rounds.BreakRemaining, 5, "Ordinary adventure retains five-second break");
        Require(rounds.Tick(4.9f, true, true) == AdventureRoundRules.Change.None && rounds.BetweenRounds && rounds.Round == 1, "Ordinary adventure does not skip its break");
        Require(rounds.Tick(.11f, true, true) == AdventureRoundRules.Change.RoundStarted && rounds.Round == 2 && !rounds.BetweenRounds, "Ordinary adventure resumes after five seconds");
        Require(rounds.StartNextRound() == AdventureRoundRules.Change.None && rounds.Round == 2, "Cannot advance an active round twice");
        rounds = new AdventureRoundRules();
        Require(rounds.Tick(.2f, false, false, false, true) == AdventureRoundRules.Change.None, "Living units prevent an exhausted round ending early");
        Require(rounds.Tick(AdventureRoundRules.Duration, false, false, false, true) == AdventureRoundRules.Change.RoundEnded && rounds.Remaining == 0, "Time zero ends the round even with living units");
        for (int team = 0; team < 2; team++)
        {
            simulation = Create(); simulation.Elixir[0] = simulation.Elixir[1] = 2;
            simulation.DefinitionFor(team, 0).Stationary = true;
            simulation.DefinitionFor(team, 0).Damage = 0;
            simulation.DefinitionFor(team, 0).Healing = 10;
            simulation.Units[0] = new DuelSimulation.Unit { Id = 1, Team = team, Kind = 0, X = 0, Y = 0, Health = 100 };
            simulation.Tick(.2f);
            Require(!simulation.Rounds.BetweenRounds && simulation.RoundOverSeconds == 0, "Either team's living stationary healer prevents early termination");
            simulation.Units[0].Health = 0; simulation.Tick(.2f);
            Require(simulation.Rounds.BetweenRounds && simulation.RoundOverSeconds > 0, "Dead unit data does not prevent exhausted round ending");
        }
        simulation = Create(); simulation.Elixir[0] = 2; simulation.Tick(.2f);
        Require(!simulation.Rounds.BetweenRounds, "Opponent can still summon");
        simulation.Elixir[1] = 2; simulation.Tick(.2f);
        Require(simulation.Rounds.BetweenRounds && simulation.Remaining > 0 && simulation.RoundOverSeconds > 0, "Energy exhaustion starts ROUND OVER before time zero");
        simulation = Create(); EndRound(simulation, false);
        Require(simulation.RoundOverSeconds == 1 && !simulation.SelectingAugment && !simulation.CanDeploy, "ROUND OVER precedes augments and blocks deployment");
        Require(!simulation.SelectAugment(0, 0, 1), "No selection during announcement");
        Advance(simulation, 4);
        Require(simulation.RoundOverSeconds > 0 && !simulation.SelectingAugment, "No cards before one second");
        Advance(simulation, 1);
        Near(simulation.AugmentSeconds, BattleAugmentRules.SelectionSeconds, "Full selection timer after announcement");
        Near(simulation.Rounds.BreakRemaining, AdventureRoundRules.BreakDuration, "Announcement does not consume next-round countdown");
        Require(simulation.SelectingAugment && !simulation.CanDeploy, "Round end begins selection and blocks deployment");
        Require(simulation.AugmentChoiceRound == 1, "Offers expose their original round");
        for (int team = 0; team < 2; team++)
            for (int slot = 0; slot < 3; slot++) Require(BattleAugmentRules.Tier(BattleAugmentRules.Effect(simulation.AugmentOffer(team, slot))) == simulation.CurrentAugmentTier, "Shared tier");
        Require(!simulation.SelectAugment(0, 0, 0) && !simulation.SelectAugment(0, 3, 1), "Stale round and invalid slot rejected");
        SetOffer(simulation, 0, 0, AugmentEffect.AttackSpeed);
        Require(simulation.SelectAugment(0, 0, 1) && !simulation.SelectAugment(0, 1, 1), "Exactly one pick per player");
        Advance(simulation, 50); Require(simulation.Rounds.Round == 1 && simulation.SelectingAugment, "Wait for opponent");
        Apply(simulation, 0, AugmentEffect.NextRoundEnergy);
        Advance(simulation, 101); Require(!simulation.SelectingAugment && simulation.AugmentPick(1) >= 0 && simulation.Rounds.Round == 2 && simulation.CanDeploy, "Timeout auto-selects and resumes immediately");
        Require(simulation.AugmentChoiceRound == 1, "Completed choice remains attributed to the previous round");
        Near(simulation.Elixir[0], 35, "Bonus energy next round"); Near(simulation.RoundEnergy[0], 35, "Energy UI maximum");
        EndRound(simulation);
        Require(simulation.AugmentChoiceRound == 2 && simulation.AugmentPick(0) == -1 && simulation.AugmentPick(1) == -1, "New selection resets picks and advances choice round");
        SetOffer(simulation, 0, 0, AugmentEffect.AttackSpeed);
        simulation.SelectAugment(0, 0, 2); simulation.SelectAugment(1, 0, 2);
        Near(simulation.Elixir[0], 25, "Energy bonus expires after one round");

        simulation = Create(); EndRound(simulation);
        int energyCode = SetOffer(simulation, 0, 0, AugmentEffect.NextRoundEnergy);
        int speedCode = SetOffer(simulation, 1, 0, AugmentEffect.AttackSpeed);
        Require(simulation.SelectAugment(1, 0, 1) && simulation.Rounds.Round == 1 && !simulation.CanDeploy, "First choice cannot resume battle");
        Require(simulation.SelectAugment(0, 0, 1) && simulation.Rounds.Round == 2 && simulation.CanDeploy && !simulation.Rounds.BetweenRounds, "Second manual choice starts next round synchronously");
        Near(simulation.Remaining, AdventureRoundRules.Duration, "Immediate start retains full round duration");
        Near(simulation.Rounds.BreakRemaining, 0, "Immediate start has no post-selection countdown");
        Near(simulation.RoundEnergy[0], 35, "Second pick energy is applied before resetting the budget");
        Near(simulation.RoundEnergy[1], 25, "Other team receives its ordinary budget");
        Require(simulation.AugmentChoiceRound == 1 && simulation.AugmentPick(0) == energyCode && simulation.AugmentPick(1) == speedCode, "Both authoritative picks survive immediate transition");
        Require(!simulation.SelectAugment(0, 0, 1) && !simulation.SelectAugment(1, 0, 2), "Late or premature choices cannot apply twice");
        Near(simulation.RoundEnergy[0], 35, "Late choice cannot repeat energy bonus");

        simulation = Create(); EndRound(simulation);
        int[] blueOffers = Enumerable.Range(0, 3).Select(slot => simulation.AugmentOffer(0, slot)).ToArray();
        int[] redOffers = Enumerable.Range(0, 3).Select(slot => simulation.AugmentOffer(1, slot)).ToArray();
        for (int step = 0; step < 160 && simulation.SelectingAugment; step++) simulation.Tick(.2f);
        Require(simulation.Rounds.Round == 2 && simulation.CanDeploy && !simulation.SelectingAugment, "Both missing choices time out into one immediate round transition");
        Require(simulation.AugmentChoiceRound == 1 && blueOffers.Contains(simulation.AugmentPick(0)) && redOffers.Contains(simulation.AugmentPick(1)), "Timeout resolves both original offers despite round advance");
        Near(simulation.Remaining, AdventureRoundRules.Duration, "Timeout transition does not consume the new round clock");

        simulation = Create();
        while (!simulation.Finished)
        {
            EndRound(simulation);
            if (simulation.Finished) break;
            Require(simulation.Rounds.Round < simulation.Rounds.ScheduledRounds, "No final round offers");
            simulation.SelectAugment(0, 0, simulation.Rounds.Round); simulation.SelectAugment(1, 0, simulation.Rounds.Round);
        }
        Require(simulation.Rounds.Round == 5 && !simulation.SelectingAugment, "Overtime final round has no augment");
        simulation = Create(); simulation.Towers[0].Health -= 100;
        while (!simulation.Finished)
        {
            EndRound(simulation);
            if (simulation.Finished) break;
            simulation.SelectAugment(0, 0, simulation.Rounds.Round); simulation.SelectAugment(1, 0, simulation.Rounds.Round);
        }
        Require(simulation.Rounds.Round == 4 && !simulation.SelectingAugment, "Normal final round has no augment");
        simulation = Create(); simulation.Towers[1].Health = 0; simulation.Tick(.2f);
        Require(simulation.Finished && !simulation.SelectingAugment, "Early castle victory has no augment");

        simulation = Create();
        for (int index = 0; index < 32; index++) simulation.Units[index] = new DuelSimulation.Unit
            { Id = index + 1, Team = 0, Kind = 0, X = -2, Y = 4, Health = 100 };
        Require(!simulation.Deploy(0, 0, -7, 2.5f, out _), "Normal deployment limit");
        Apply(simulation, 0, AugmentEffect.Unlimited);
        Require(simulation.Deploy(0, 0, -7, 2.5f, out _), "Unlimited bypasses placement limit");

        simulation = Create(); Apply(simulation, 1, AugmentEffect.StationaryTaunt, 1);
        simulation.DefinitionFor(0, 0).Range = 3;
        var attacker = new DuelSimulation.Unit { Id = 1, Team = 0, Kind = 0, X = 8, Y = 2, Health = 100, TowerTarget = 1 };
        var taunter = new DuelSimulation.Unit { Id = 2, Team = 1, Kind = 1, X = 7, Y = 2, Health = 100 };
        simulation.Units[0] = attacker; simulation.Units[1] = taunter; simulation.Tick(.2f);
        Require(attacker.TargetId == taunter.Id, "Taunt interrupts castle focus");
        Near(taunter.X, 7, "Stationary X"); Near(taunter.Y, 2, "Stationary Y");
        simulation.DefinitionFor(0, 0).Priority = TargetPriority.Buildings; attacker.TargetId = 0; simulation.Tick(.2f);
        Require(attacker.TargetId == taunter.Id, "Taunt overrides building priority");
#if UNITY_EDITOR
        UnityEngine.Debug.Log("[Augments] PASS: tier distribution, offers, all effects, stacks, twins, immediate round resume, retained choice round, ordinary adventure break, round budgets, selection timeout, final rounds, deployment limits and taunt.");
#endif
    }
}
