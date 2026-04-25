using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnApplyPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        WriteIt(power.StackType is PowerStackType.Counter || amount > 0
            ? $"> {FormatCreature(target)} **received** [`{power.Title.GetFormattedText()} {(int) amount}`] <\\"
            : $"> {FormatCreature(target)} **received** [`{power.Title.GetFormattedText()}`] <\\");
        _db.OnApplyPower(power, target, amount, applier, cardSource);
    }

    public void OnRemovePower(PowerModel? power)
    {
        if (power == null) return;
        WriteIt($"> {FormatCreature(power.Owner)} **cleared** [`{power.Title.GetFormattedText()}`] <\\");
    }
}