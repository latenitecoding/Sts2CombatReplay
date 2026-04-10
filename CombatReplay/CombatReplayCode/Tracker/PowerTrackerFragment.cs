using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnApplyPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        WriteIt($"> {FormatCreature(target)} **received** [`{power.Title.GetFormattedText()} {(int) amount}`] <\\");
        _db.OnApplyPower(power, target, amount, applier, cardSource);
    }
}