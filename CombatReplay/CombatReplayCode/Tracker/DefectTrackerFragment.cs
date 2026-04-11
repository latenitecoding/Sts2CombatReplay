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
        WriteBefore(
            $"> {FormatPlayer(player)} **channeled** {FormatOrb(orb)} <\\",
            ReplayLogger.MsgType.OrbEvoked);
        _db.TotalOrbsChanneled += 1;
        return Task.CompletedTask;
    }

    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        if (!LocalContext.IsMe(orb.Owner)) return Task.CompletedTask;
        BufferBefore(
            $"> {FormatPlayer(orb.Owner)} **evoked** {FormatOrb(orb, useEvokeVal: true)} <\\",
            ReplayLogger.MsgType.OrbEvoked,
            ReplayLogger.MsgType.RpoHit);
        _db.TotalOrbsEvoked += 1;
        return Task.CompletedTask;
    }

    public void OnOrbPassive(OrbModel orb)
    {
        WriteIt($"> {FormatOrb(orb)} **triggered** <\\");
        if (orb is DarkOrb)
        {
            WriteIt($"> `{orb.Title.GetFormattedText()}` **gained** [`Damage {(int) orb.PassiveVal}`] <\\");
        }
    }

    private string FormatOrb(OrbModel orb, bool useEvokeVal = false)
    {
        if (orb is LightningOrb or GlassOrb or DarkOrb)
        {
            return $"`{orb.Title.GetFormattedText()} Orb` [`Damage {(int) (useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]";
        }
        else if (orb is FrostOrb)
        {
            return $"`{orb.Title.GetFormattedText()} Orb` [`Block {(int) (useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]";
        }
        else if (orb is PlasmaOrb)
        {
            return $"`{orb.Title.GetFormattedText()} Orb` [`Energy {(int) (useEvokeVal ? orb.EvokeVal : orb.PassiveVal)}`]";
        }
        return $"`{orb.Title.GetFormattedText()} Orb`";
    }
}