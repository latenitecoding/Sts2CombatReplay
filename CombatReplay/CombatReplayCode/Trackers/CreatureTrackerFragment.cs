using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CombatReplay.CombatReplayCode.Trackers;

public partial class CombatReplayTracker
{
    public Task OnCreatureAdded(Creature creature)
    {
        var designation = (creature.IsEnemy)
            ? "Enemy"
            : ((creature.IsPet)
                ? "Pet"
                : ((creature.IsPlayer)
                    ? "Player"
                    : "Other"));
        if (LocalContext.IsMe(creature))
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <--- THIS IS ME <\\");
        }
        else if (creature is { IsPet: true } && LocalContext.IsMe(creature.PetOwner))
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <--- THIS IS MY PET <\\");
        }
        else
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <\\");
        }

        _db.AddCombatCreature(creature, FormatCreature(creature));
        if (creature.IsEnemy)
        {
            _db.TotalEnemiesFought += 1;
        }
        return Task.CompletedTask;
    }
    
    public void RecordEnemyIntent(Creature owner, MoveState state)
    {
        if (!_db.IsInCombat()) return;
        var intentions = string.Join(
            ", ",
            state.Intents.Select(intention => {
                if (intention is AttackIntent attackIntention)
                {
                    var dmg = attackIntention.DamageCalc?.Invoke() ?? -1;
                    if (attackIntention.Repeats > 1)
                    {
                        return $"`{intention.IntentType.ToString()} {(int) dmg}x{attackIntention.Repeats}`";
                    }
                    else
                    {
                        return $"`{intention.IntentType.ToString()} {(int) dmg}`";
                    }
                }
                return $"`{intention.IntentType.ToString()}`";
            }));
        WriteIt($"> {FormatCreature(owner)} **intends** `{state.Id}` [{intentions}] <\\");
    }
    
    public override Task AfterBlockBroken(Creature creature)
    {
        WriteIt($"> {FormatCreature(creature)} **broken** <\\");
        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        WriteIt($"> {FormatCreature(creature)} **defeated** <\\");
        return Task.CompletedTask; 
    }
    
    private static string FormatCreature(Creature creature)
    {
        var shownHp = (creature.ShowsInfiniteHp) ? "Inf/Inf" : $"{creature.CurrentHp}/{creature.MaxHp}";
        return (creature.CombatId != null)
            ? $"`{creature.Name}` (`{creature.CombatId}`) [`{creature.Block}|{shownHp}` bHP]"
            : $"`{creature.Name}` (`{creature.ModelId}`) [`{creature.Block}|{shownHp}` bHP]";
    }
}