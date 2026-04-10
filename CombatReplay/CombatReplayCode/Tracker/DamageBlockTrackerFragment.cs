using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterBlockGained(Creature creature, Decimal amount, ValueProp props, CardModel? cardSource)
    {
        WriteIt(cardSource != null
            ? $"> {FormatCreature(creature)} **used** `{cardSource.Title}` [`Block {amount}`] <\\"
            : $"> {FormatCreature(creature)} **gained** [`Block {amount}`] <\\");

        if (!LocalContext.IsMe(creature.Player)) return Task.CompletedTask;
        
        _db.AddCombatBlockGained((int) amount);
        if (cardSource != null)
        {
            _db.AddBlockGainedByCard(cardSource.Title, (int) amount);
        }
        else
        {
            _db.TotalRelicPowerBlock += (int) amount;
        }
        return Task.CompletedTask;
    }

    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)} <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **targeted** {FormatCreature(target)} <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **received** [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] <\\");
        }
        
        _db.OnCombatDamageDealt(dealer, target, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
        return Task.CompletedTask;
    }
    
    // if applying damages results in combat ending, then combat ends without AfterDamageReceived firing
    // so this is used to ensure that all possible damage events are observed
    public override Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, Decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var permittedBlock = (dealer != null && dealer.Name == target.Name) ? 0 : target.Block;
        if (permittedBlock + target.CurrentHp > (int) amount)
        {
            return Task.CompletedTask;
        }
        
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} [`Damage {target.Block}|{(int) amount - target.Block}`] **hit** {FormatCreature(target)} <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {target.Block}|{(int) amount - target.Block}`] **targeted** {FormatCreature(target)} <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **received** [`Damage {target.Block}|{(int) amount - target.Block}`] <\\");
        }

        // this is called when damage is lethal; amount should always be the full amount while the others should reflect damage dealt to the creature
        // the total damage tracks amount that could have been achieved if monster had the hp
        _db.OnCombatDamageDealt(dealer, target, cardSource, (int) amount, target.CurrentHp, target.Block);
        return Task.CompletedTask;
    }
}