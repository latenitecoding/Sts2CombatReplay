using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.ValueProps;

namespace CombatReplay.CombatReplayCode.Trackers;

public partial class CombatReplayTracker
{
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

        RecordDamageTotals(target, dealer, cardSource, (int) amount, target.CurrentHp, target.Block);
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
        
        RecordDamageTotals(target, dealer, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
        return Task.CompletedTask;
    }
    
    public override Task AfterBlockGained(Creature creature, Decimal amount, ValueProp props, CardModel? cardSource)
    {
        WriteIt(cardSource != null
            ? $"> {FormatCreature(creature)} **used** `{cardSource.Title}` [`Block {amount}`] <\\"
            : $"> {FormatCreature(creature)} **gained** [`Block {amount}`] <\\");

        if (!LocalContext.IsMe(creature.Player)) return Task.CompletedTask;
        
        _db.AddCombatBlockGained((int) amount);
        if (cardSource != null)
        {
            _db.AddBlockGained(cardSource.Title, (int) amount);
        }
        else
        {
            _db.TotalRelicPowerBlock += (int) amount;
        }
        return Task.CompletedTask;
    }
    
    private void RecordDamageTotals(Creature target, Creature? dealer, CardModel? cardSource, int totalDamage, int? trueDamage, int? blockedDamage)
    {
        if (target.IsEnemy && IsMeOrMine(dealer))
        {
            _db.AddCombatDamageDealt(totalDamage, trueDamage, blockedDamage);
            if (dealer is { IsPet: true })
            {
                _db.TotalPetDamage += totalDamage;
            }
        }
        else if (IsMeOrMine(target))
        {
            if (target is { IsPet: true })
            {
                _db.TotalPetDamageReceived += Math.Min(totalDamage, target.Block + target.CurrentHp);
            }
            else
            {
                _db.AddCombatDamageReceived(totalDamage, trueDamage, blockedDamage);
                if (dealer != null && LocalContext.IsMe(dealer.Player))
                {
                    _db.TotalSelfDamage += totalDamage;
                }
            }
        }       
        if (cardSource != null && LocalContext.IsMe(cardSource.Owner))
        {
            _db.AddDamageDealt(cardSource.Title, totalDamage);
        }
        else if (cardSource == null && dealer != null && LocalContext.IsMe(dealer.Player))
        {
            _db.TotalRelicPowerDamage += totalDamage;
        }
    }
}