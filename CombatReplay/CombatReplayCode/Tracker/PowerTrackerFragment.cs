using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Powers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnApplyPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        WriteIt(IsNonStackablePower(power, target, (int)amount)
            ? $"> {FormatCreature(target)} **received** [`{power.Title.GetFormattedText()}`] <\\"
            : $"> {FormatCreature(target)} **received** [`{power.Title.GetFormattedText()} {(int)amount}`] <\\");
        _db.OnApplyPower(power, target, amount, applier, cardSource);
    }

    public void OnRemovePower(PowerModel? power)
    {
        if (power == null) return;
        BufferIt($"> {FormatCreature(power.Owner)} **cleared** [`{power.Title.GetFormattedText()}`] <\\", ReplayLogger.MsgType.PowerCleared);
    }

    private string FormatPower(PowerModel power)
    {
        return IsNonStackablePower(power, power.Target, power.Amount)
            ? $"`{power.Title.GetFormattedText()}`"
            : $"`{power.Title.GetFormattedText()} {power.Amount}`";
    }

    private bool IsNonStackablePower(PowerModel power, Creature? target, int amount)
    {
        if (power is StrengthPower) return false;
        return power.StackType is not PowerStackType.Counter || (amount < 0 && target != null && !target.HasPower(power.Id));
    }
}