using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterOstyRevived(Creature osty)
    {
        if (IsMeOrMine(osty))
        {
            WriteIt($"> My {FormatCreature(osty)} **revived** <\\");
            _db.TotalOstyRevives += 1;
        }
        else
        {
            WriteIt($"> Another {FormatCreature(osty)} **revived** <\\");
        }
        return Task.CompletedTask;
    }
    
    public override Task AfterSummon(PlayerChoiceContext ctx, Player summoner, Decimal amount)
    {
        if (!LocalContext.IsMe(summoner)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(summoner)} **summoned** `{(int) amount}` <\\");
        _db.TotalSummoned += (int) amount;
        return Task.CompletedTask;
    }
}