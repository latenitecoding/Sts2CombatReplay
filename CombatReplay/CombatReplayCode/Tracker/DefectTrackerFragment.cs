using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Orbs;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        // channeled events are triggered after evoke events even though the channel occurs first in game
        WriteBefore(
            $"> {FormatPlayer(player)} **channeled** {FormatOrb(orb)}",
            preceding: ReplayLogger.MsgType.OrbEvoked);
        _db.TotalOrbsChanneled++;
        return Task.CompletedTask;
    }

    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        if (!LocalContext.IsMe(orb.Owner)) return Task.CompletedTask;
        // the damage from an evoke is resolved before the evoke trigger even though the evoke occurs first in game
        BufferBefore(
            $"> {FormatPlayer(orb.Owner)} **evoked** {FormatOrb(orb, useEvokeVal: true)}",
            ReplayLogger.MsgType.OrbEvoked,
            preceding: ReplayLogger.MsgType.RpoHit,
            expecting: ReplayLogger.MsgType.OrbChanneled);
        _db.TotalOrbsEvoked++;
        return Task.CompletedTask;
    }

    public void OnOrbPassive(OrbModel orb)
    {
        WriteIt($"> {FormatOrb(orb)} **triggered**");
        if (orb is DarkOrb)
        {
            // the dark orb is the only ambiguous orb because it accumulates damage rather than gaining it
            // other orbs have an in-game effect that will be logged by the other event handlers
            WriteIt($"> `{orb.Title.GetFormattedText()}` **gained** [`Damage {(int) orb.PassiveVal}`]");
        }
    }

    private string FormatOrb(OrbModel orb, bool useEvokeVal = false)
    {
        return orb switch
        {
            LightningOrb or GlassOrb or DarkOrb => $"`{orb.Title.GetFormattedText()} Orb` [`Damage {(int)(useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]",
            FrostOrb => $"`{orb.Title.GetFormattedText()} Orb` [`Block {(int)(useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]",
            PlasmaOrb => $"`{orb.Title.GetFormattedText()} Orb` [`Energy {(int)(useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]",
            _ => $"`{orb.Title.GetFormattedText()} Orb`",
        };
    }
}