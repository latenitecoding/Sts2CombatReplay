using System.Formats.Tar;
using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterBlockBroken(Creature creature)
    {
        // the necrobinder block broken event is triggered after osty takes damage even though it occurs first in game
        BufferBefore(
            $"> {FormatCreature(creature)} **broken** <\\", ReplayLogger.MsgType.BlockBroken, 
            ReplayLogger.MsgType.PetWasHit);
        return Task.CompletedTask;
    }

    public override Task AfterBlockGained(Creature creature, Decimal amount, ValueProp props, CardModel? cardSource)
    {
        BufferIt(cardSource != null
            ? $"> {FormatCreature(creature)} **used** `{cardSource.Title}` [`Block {amount}`] <\\"
            : $"> {FormatCreature(creature)} **gained** [`Block {amount}`] <\\",
            ReplayLogger.MsgType.BlockGained);

        if (!LocalContext.IsMe(creature.Player)) return Task.CompletedTask;
        
        _db.AddCombatBlockGained(cardSource, (int) amount);
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (result.TotalDamage == 0) return Task.CompletedTask;
        if (dealer != null && target is { IsPet: true } and not { PetOwner: null } &&
            LocalContext.IsMe(target.PetOwner))
        {
            // have to buffer hits against the pet because they are logged out of order
            // current order is osty damage -> necro block broken -> necro damage (remaining)
            // order should be necro damage -> necro block broken -> osty damage -> necro damage (remaining)
            BufferIt(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)} <\\",
                ReplayLogger.MsgType.PetWasHit);
            _db.OnCombatDamageDealt(dealer, target, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
            return Task.CompletedTask;
        }
        
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer is { IsPlayer: true })
        {
            // some player RPO hits only trigger BeforeDamageReceived and others trigger both
            // if this is one that triggers both, we need to overwrite the prior playerRpoHit
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            // this must also be buffered since damage from orbs are logged before evoke even though evoke occurs first
            WriteIt(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)} <\\",
                ReplayLogger.MsgType.BeforeDamage);
        }
        else if (dealer != null)
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            WriteBefore(
                $"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)} <\\",
                ReplayLogger.MsgType.BlockBroken);
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **targeted** {FormatCreature(target)} <\\");
        }
        else
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            WriteBefore(
                $"> {FormatCreature(target)} **took** [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] <\\", 
                ReplayLogger.MsgType.BlockBroken);
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
        if (permittedBlock + target.CurrentHp > (int) amount && !playerRpoHit)
        {
            return Task.CompletedTask;
        }

        if (dealer != null && target is { IsPet: true } && LocalContext.IsMe(target.PetOwner))
        {
            // have to buffer hits against the pet because they are logged out of order
            // current order is osty damage -> necro block broken -> necro damage (remaining)
            // order should be necro damage -> necro block broken -> osty damage -> necro damage (remaining)
            BufferIt(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target)} <\\",
                    ReplayLogger.MsgType.PetWasHit);
            _db.OnCombatDamageDealt(dealer, target, cardSource, (int) amount, target.CurrentHp, target.Block);
            return Task.CompletedTask;
        }

        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer is { IsPlayer : true })
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            // this must also be buffered since damage from orbs are logged before evoke even though evoke occurs first
            BufferBefore(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target)} <\\",
                ReplayLogger.MsgType.BeforeDamage | ReplayLogger.MsgType.RpoHit,
                ReplayLogger.MsgType.BlockBroken);
        }
        else if (dealer != null)
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            WriteBefore(
                $"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target)} <\\",
                ReplayLogger.MsgType.BlockBroken);
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **targeted** {FormatCreature(target)} <\\");
        }
        else
        {
            // damage dealt logs block broken on creatures before damage even though it should be damage and then block broken
            WriteBefore(
                $"> {FormatCreature(target)} **took** [`Damage {target.Block}|{(int) amount - target.Block}`] <\\",
                ReplayLogger.MsgType.BlockBroken);
        }

        // this is called when damage is lethal; amount should always be the full amount while the others should reflect damage dealt to the creature
        // the total damage tracks amount that could have been achieved because the player wants to see their damage ceiling
        _db.OnCombatDamageDealt(dealer, target, cardSource, (int) amount, target.CurrentHp, target.Block);
        return Task.CompletedTask;
    }
}