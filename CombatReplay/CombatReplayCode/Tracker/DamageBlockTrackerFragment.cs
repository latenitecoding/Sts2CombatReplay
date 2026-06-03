using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.ValueProps;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterBlockBroken(Creature creature)
    {
        // the necrobinder block broken event is triggered after osty takes damage even though it occurs first in game
        // most damage events are triggered after block broken
        BufferBefore(
            $"> {FormatCreature(creature)} **broken**",
            ReplayLogger.MsgType.BlockBroken, 
            preceding: ReplayLogger.MsgType.PetWasHit,
            expecting: ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.RpoHit | ReplayLogger.MsgType.TookDamage);
        return Task.CompletedTask;
    }

    public override Task AfterBlockGained(Creature creature, Decimal amount, ValueProp props, CardModel? cardSource)
    {
        WriteBefore(cardSource != null
            ? $"> {FormatCreature(creature)} **used** `{cardSource.Title}` [`Block {(int)amount}`]"
            : $"> {FormatCreature(creature)} **gained** [`Block {(int)amount}`]",
            ReplayLogger.MsgType.TookDamage);

        if (!LocalContext.IsMe(creature.Player)) return Task.CompletedTask;
        
        _db.AddCombatBlockGained(cardSource, (int) amount);
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (result.TotalDamage == 0) return Task.CompletedTask;
        
        if (dealer != null && target is { IsPet: true } && LocalContext.IsMe(target.PetOwner))
        {
            // have to buffer hits against the pet because they are logged out of order
            // current order is osty damage -> necro block broken -> necro damage (remaining)
            // order should be necro damage -> necro block broken -> osty damage -> necro damage (remaining)
            var (_, found) = BufferIt(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)}",
                ReplayLogger.MsgType.PetWasHit,
                overwriting: ReplayLogger.MsgType.PetWasHit,
                expecting: ReplayLogger.MsgType.BlockBroken | ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.PowerApplied);
            
            if (found) return Task.CompletedTask;
            
            _db.OnCombatDamageDealt(dealer, target, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
            return Task.CompletedTask;
        }
        
        if (dealer != null && cardSource != null)
        {
            BufferBefore(
                dealer == target
                    ? $"> {FormatCreature(dealer)} **suffered** [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`]"
                    : $"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **against** {FormatCreature(target)}",
                ReplayLogger.MsgType.TookDamage,
                preceding: ReplayLogger.MsgType.BlockBroken | ReplayLogger.MsgType.PowerApplied,
                expecting: ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.PowerApplied);
        }
        else if (dealer is { IsPlayer: true })
        {
            // some player RPO hits only trigger BeforeDamageReceived and others trigger both
            // if this is one that triggers both, we need to overwrite the prior playerRpoHit
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            // this must also be buffered since damage from orbs are logged before evoke even though evoke occurs first
            var (_, found) = BufferIt(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)}",
                ReplayLogger.MsgType.RpoHit,
                overwriting: ReplayLogger.MsgType.BeforeDamage,
                expecting: ReplayLogger.MsgType.OrbEvoked | ReplayLogger.MsgType.PowerApplied);
            
            if (found) return Task.CompletedTask; // damage event already logged; this prevents double counting
        }
        else if (dealer != null)
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            BufferBefore(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)}",
                ReplayLogger.MsgType.PlayerOrEnemyWasHit,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        else if (cardSource != null)
        {
            BufferBefore(
                $"> `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **targeted** {FormatCreature(target)}",
                ReplayLogger.MsgType.PlayerOrEnemyWasHit,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        else
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            BufferBefore(
                $"> {FormatCreature(target)} **took** [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`]", 
                ReplayLogger.MsgType.TookDamage,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        
        _db.OnCombatDamageDealt(dealer, target, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
        return Task.CompletedTask;
    }
    
    // if applying damages results in combat ending, then combat ends without AfterDamageReceived firing
    // so this is used to ensure that all possible damage events are observed
    public override Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, Decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        // if the damage is non-lethal, then this event will be called and then AfterDamageReceived will be called
        // this ensures that this method terminates immediately if the damage is non-lethal
        // since players can block for their pets, it makes sense to add the pet owner's block to the pet's block
        // however, the game calculates that in advance and separates the damage into three separate events
        //  - damage dealt to pet owner's block
        //  - damage dealt to pet
        //  - remaining damage dealt to pet owner
        // as such, any damage to the pet would have already had the pet owner's block deducted from it
        var permittedBlock = dealer != null && dealer != target ? target.Block : 0;
        // some playerRpoHits only trigger BeforeDamageReceived even if they are non-lethal
        var playerRpoHit = dealer is { IsPlayer : true } && cardSource == null && target.IsEnemy;
        
        // have to track pet damage first and separately
        if (dealer != null && target is not { Player: null } && target.Player is not { Osty: null } && LocalContext.IsMe(target))
        {
            var ostyDamage = Math.Min(Math.Max((int)amount - target.Block - target.Player.Osty.Block, 0), target.Player.Osty.CurrentHp);
            permittedBlock += ostyDamage;
            
            if (ostyDamage > 0)
            {
                BufferIt(
                    $"> {FormatCreature(dealer)} [`Damage {target.Player.Osty.Block}|{ostyDamage}`] **hit** {FormatCreature(target.Player.Osty, isDefeated: ostyDamage >= target.Player.Osty.CurrentHp)}",
                    ReplayLogger.MsgType.PetWasHit,
                    overwriting: ReplayLogger.MsgType.None,
                    expecting: ReplayLogger.MsgType.BlockBroken | ReplayLogger.MsgType.PetWasHit | ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.PowerApplied);
                _db.OnCombatDamageDealt(dealer, target.Player.Osty, cardSource, Math.Max((int)amount - target.Block, 0), ostyDamage, target.Player.Osty.Block);
                return Task.CompletedTask;
            }
        }
        
        if (permittedBlock + target.CurrentHp > (int) amount && !playerRpoHit)
        {
            return Task.CompletedTask;
        }
        
        if (target.CurrentHp > 1 && target.Powers.Any(power => power is SlipperyPower or IntangiblePower))
        {
            return Task.CompletedTask;
        }

        if (dealer != null && target is { IsPet: true } && LocalContext.IsMe(target.PetOwner))
        {
            // was implemented for a previous version of how osty damage was handled
            // keeping here in case future pets appear in the game
            BufferIt(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target, isDefeated: true)}",
                ReplayLogger.MsgType.PetWasHit,
                overwriting: ReplayLogger.MsgType.None,
                expecting: ReplayLogger.MsgType.BlockBroken | ReplayLogger.MsgType.PetWasHit | ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.PowerApplied);
            _db.OnCombatDamageDealt(dealer, target, cardSource, (int) amount, target.CurrentHp, target.Block);
            return Task.CompletedTask;
        }

        if (dealer != null && cardSource != null)
        {
            BufferBefore(
                dealer == target
                    ? $"> {FormatCreature(dealer)} **suffered** [`Damage {target.Block}|{(int) amount - target.Block}`]"
                    : $"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **against** {FormatCreature(target, isDefeated: true)}",
                ReplayLogger.MsgType.TookDamage,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        else if (dealer is { IsPlayer : true })
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            // this must also be buffered since damage from orbs are logged before evoke even though evoke occurs first
            BufferBefore(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target, isDefeated: true)}",
                ReplayLogger.MsgType.BeforeDamage | ReplayLogger.MsgType.RpoHit,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.OrbEvoked | ReplayLogger.MsgType.PowerApplied | ReplayLogger.MsgType.RpoHit);
        }
        else if (dealer != null)
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            BufferBefore(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target, isDefeated: true)}",
                ReplayLogger.MsgType.PlayerOrEnemyWasHit,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        else if (cardSource != null)
        {
            BufferBefore(
                $"> `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **targeted** {FormatCreature(target, isDefeated: true)}",
                ReplayLogger.MsgType.PlayerOrEnemyWasHit,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied);
        }
        else
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            BufferBefore(
                $"> {FormatCreature(target, isDefeated: true)} **took** [`Damage {target.Block}|{(int) amount - target.Block}`]",
                ReplayLogger.MsgType.TookDamage,
                preceding: ReplayLogger.MsgType.BlockBroken,
                expecting: ReplayLogger.MsgType.PowerApplied | ReplayLogger.MsgType.TookDamage);
        }

        // this is called when damage is lethal; amount should always be the full amount while the others should reflect damage dealt to the creature
        // the total damage tracks amount that could have been achieved because the player wants to see their damage ceiling
        _db.OnCombatDamageDealt(dealer, target, cardSource, (int) amount, Math.Max(Math.Min(target.CurrentHp, (int)amount - permittedBlock), 0), target.Block);
        return Task.CompletedTask;
    }
}