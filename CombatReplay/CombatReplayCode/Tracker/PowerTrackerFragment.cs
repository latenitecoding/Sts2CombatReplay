using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void RecordPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0) return;
        
        var isMyCard = cardSource != null && LocalContext.IsMe(cardSource.Owner);
        
        if (power.Title.GetFormattedText().Contains("Strength") && (IsMeOrMine(target) || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **buffed** [`Strength {(int) amount}`] <\\");
            _db.TotalStrengthGained += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Vulnerable") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Vulnerable {(int) amount}`] <\\");
            _db.TotalVulnerableApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Weak") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Weak {(int) amount}`] <\\");
            _db.TotalWeakApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Poison") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Poison {(int) amount}`] <\\");
            _db.TotalPoisonApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Doom") && (IsMeOrMine(applier) || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Doom {(int) amount}`] <\\");
            _db.TotalDoomApplied += (int) amount;
        }
    }
}