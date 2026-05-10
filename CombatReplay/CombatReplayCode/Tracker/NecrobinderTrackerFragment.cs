using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterOstyRevived(Creature osty)
    {
        if (osty is { IsPet: true } && LocalContext.IsMe(osty.PetOwner))
        {
            BufferIt(
                $"> My {FormatCreature(osty)} **revived**",
                ReplayLogger.MsgType.ReviveCreature,
                overwriting: ReplayLogger.MsgType.None,
                expecting: ReplayLogger.MsgType.Summon);
            _db.TotalOstyRevives += 1;
        }
        else
        {
            WriteIt($"> Another {FormatCreature(osty)} **revived**");
        }
        return Task.CompletedTask;
    }
    
    public override Task AfterSummon(PlayerChoiceContext ctx, Player summoner, Decimal amount)
    {
        if (!LocalContext.IsMe(summoner)) return Task.CompletedTask;
        WriteBefore(
            $"> {FormatPlayer(summoner)} **summoned** `{(int) amount}`", 
            preceding: ReplayLogger.MsgType.HealCreature | ReplayLogger.MsgType.GainMaxHp);
        _db.TotalSummoned += (int) amount;
        return Task.CompletedTask;
    }
}