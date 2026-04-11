using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        BufferBefore(
            $"> {FormatPlayer(player)} **channeled** `{orb.Title.GetFormattedText()}` <\\",
            ReplayLogger.MsgType.OrbChanneled, 
            ReplayLogger.MsgType.OrbEvoked);
        _db.TotalOrbsChanneled += 1;
        return Task.CompletedTask;
    }

    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        if (!LocalContext.IsMe(orb.Owner)) return Task.CompletedTask;
        BufferBefore(
            $"> {FormatPlayer(orb.Owner)} **evoked** `{orb.Title.GetFormattedText()}` <\\",
            ReplayLogger.MsgType.OrbEvoked,
            ReplayLogger.MsgType.RpoHit);
        _db.TotalOrbsEvoked += 1;
        return Task.CompletedTask;
    }
}