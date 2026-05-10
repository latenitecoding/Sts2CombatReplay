using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        WriteBefore(
            $"> {FormatCreature(creature)} **defeated**",
            preceding: ReplayLogger.MsgType.PowerCleared);
        return Task.CompletedTask; 
    }

    public Task OnAddCreature(Creature creature)
    {
        if (LocalContext.IsMe(creature))
        {
            WriteIt($"> {(creature.Player != null ? FormatPlayer(creature.Player) : "Player")} **as** {FormatCreature(creature)} **present** <!-- THIS IS ME -->");
        }
        else if (creature is { IsPet: true } && LocalContext.IsMe(creature.PetOwner))
        {
            WriteIt($"> Pet {FormatCreature(creature)} **present** <!-- THIS IS MY PET -->");
        }
        else
        {
            WriteIt($"> Enemy {FormatCreature(creature)} **present**");
        }

        if (creature is not { Player: null })
        {
            var potions = string.Join(", ", creature.Player.Potions.Select(potion => $"`{potion.Title.GetFormattedText()}`"));
            WriteIt($"> {FormatPlayer(creature.Player)} {(LocalContext.IsMe(creature.Player) ? "**have**" : "**has**")} [{potions}] potions");
            
            var relics = string.Join(", ", creature.Player.Relics.Select(relic => $"`{relic.Title.GetFormattedText()}`"));
            WriteIt($"> {FormatPlayer(creature.Player)} {(LocalContext.IsMe(creature.Player) ? "**have**" : "**has**")} [{relics}] relics");
        }

        _db.OnAddCreature(creature, FormatCreature(creature));
        return Task.CompletedTask;
    }

    public void OnCreatureGainMaxHp(Creature creature, Decimal amount)
    {
        // because of ascension levels, after the tutorial run, all characters start at 0 HP and then heal in the first room
        // this first room of healing that sets the character to their starting HP shouldn't be logged as healing
        if (_db is { CurrentRoom: <= 1, CurrentCombat: 0 }) return;
        
        BufferIt(
            $"> {FormatCreature(creature, currentHp: creature.CurrentHp + (int) amount, maxHp: creature.MaxHp + (int) amount)} **gained** `{amount}` HP",
            ReplayLogger.MsgType.GainMaxHp,
            overwriting: ReplayLogger.MsgType.None,
            expecting: ReplayLogger.MsgType.HealCreature | ReplayLogger.MsgType.ReviveCreature | ReplayLogger.MsgType.Summon);
        
        var isMyPet = creature is { IsPet: true } && LocalContext.IsMe(creature.PetOwner);
        if (!LocalContext.IsMe(creature) && !isMyPet) return;
        
        _db.TotalHpHealed += (int) amount;
    }
    
    public void OnCreatureHeal(Creature creature, Decimal amount)
    {
        // because of ascension levels, after the tutorial run, all characters start at 0 HP and then heal in the first room
        // this first room of healing that sets the character to their starting HP shouldn't be logged as healing
        if (_db is { CurrentRoom: <= 1, CurrentCombat: 0 }) return;
        // ignore if called after gaining max HP
        if (CheckIt(ReplayLogger.MsgType.GainMaxHp)) return;
        
        var trueAmount = Math.Min((int) amount, creature.MaxHp - creature.CurrentHp);
        BufferIt(
            $"> {FormatCreature(creature, currentHp: creature.CurrentHp + trueAmount)} **healed** `{trueAmount}` HP",
            ReplayLogger.MsgType.HealCreature,
            overwriting: ReplayLogger.MsgType.None,
            expecting: ReplayLogger.MsgType.ReviveCreature | ReplayLogger.MsgType.Summon);
        
        if (!LocalContext.IsMe(creature)) return;
        _db.TotalHpHealed += trueAmount;
    }
    
    public void OnCreatureLoseMaxHp(Creature creature, Decimal amount)
    {
        // because of ascension levels, after the tutorial run, all characters start at 0 HP and then heal in the first room
        // this first room of healing that sets the character to their starting HP shouldn't be logged as healing
        if (_db is { CurrentRoom: <= 1, CurrentCombat: 0 }) return;
        
        var maxHp =  Math.Max(creature.MaxHp - (int)amount, 0);
        var currentHp = Math.Min(creature.CurrentHp, creature.MaxHp);
        
        WriteIt($"> {FormatCreature(creature, currentHp: currentHp, maxHp: maxHp)} **lost** `{amount}` HP");
    }
    
    public void OnCreatureStunned(Creature creature)
    {
        WriteIt($"> {FormatCreature(creature)} **stunned**");
    }

    public void OnRollMove(Creature owner, MoveState state)
    {
        // this action is called twice when entering a room with combat; this filters out the first
        if (!_db.IsInCombat()) return;
        var intentions = string.Join(
            ", ",
            state.Intents.Select(intention => {
                if (intention is not AttackIntent attackIntention) return $"`{intention.IntentType.ToString()}`";
                var dmg = attackIntention.DamageCalc?.Invoke() ?? -1;
                return attackIntention.Repeats > 1
                    ? $"`{intention.IntentType.ToString()} {(int) dmg}x{attackIntention.Repeats}`"
                    : $"`{intention.IntentType.ToString()} {(int) dmg}`";
            }));
        WriteIt($"> {FormatCreature(owner)} **intends** `{state.Id}` [{intentions}]");
    }
   
    private static string FormatCreature(Creature creature, int currentHp = -1, int maxHp = -1, bool isDefeated = false)
    {
        var shownHp = (creature is not { HpDisplay: HpDisplay.Normal })
            ? "Inf/Inf"
            : $"{(isDefeated ? 0 : (currentHp >= 0 ? currentHp : creature.CurrentHp))}/{(maxHp >= 0 ? maxHp : creature.MaxHp)}";
        var creatureName = creature.Name.Replace("#", "\\#");
        return (creature.CombatId != null)
            ? $"`{creatureName}` (`{creature.CombatId}`) [`{creature.Block}|{shownHp}` bHP]"
            : $"`{creatureName}` (`{creature.ModelId}`) [`{creature.Block}|{shownHp}` bHP]";
    }
    
    private static string FormatPlayer(Player player)
    {
        if (LocalContext.IsMe(player)) return "I";
        return (player.Creature.CombatId != null)
            ? $"{player.Character.Title.GetFormattedText()} (`{player.Creature.CombatId}`)"
            : $"{player.Character.Title.GetFormattedText()} (`{player.NetId}`)";
    }
}