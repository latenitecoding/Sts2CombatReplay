using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnApplyPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0) return;
        
        var isMyCard = LocalContext.IsMine(cardSource);
        var imTargeted = LocalContext.IsMe(target) || (target is { IsPet: true }  && LocalContext.IsMe(target.PetOwner));
        var iApplied = LocalContext.IsMe(applier) || isMyCard;
        
        if (power.Title.GetFormattedText().Contains("Strength") && (imTargeted || isMyCard))
        {
            WriteIt($"> {FormatCreature(target)} **buffed** [`Strength {(int) amount}`] <\\");
        }
        else if (power.Title.GetFormattedText().Contains("Vulnerable") && target.IsEnemy && iApplied)
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Vulnerable {(int) amount}`] <\\");
        }
        else if (power.Title.GetFormattedText().Contains("Weak") && target.IsEnemy && iApplied)
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Weak {(int) amount}`] <\\");
        }
        else if (power.Title.GetFormattedText().Contains("Poison") && target.IsEnemy && iApplied)
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Poison {(int) amount}`] <\\");
        }
        else if (power.Title.GetFormattedText().Contains("Doom") && iApplied)
        {
            WriteIt($"> {FormatCreature(target)} **debuffed** [`Doom {(int) amount}`] <\\");
        }

        _db.OnApplyPower(power, target, amount, applier, cardSource);
    }
}