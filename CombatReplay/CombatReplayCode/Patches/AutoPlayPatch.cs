using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class AutoPlayPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardCmd), "AutoPlay",
            new Type[]
            {
                typeof(PlayerChoiceContext),
                typeof(CardModel),
                typeof(Creature),
                typeof(AutoPlayType),
                typeof(bool),
                typeof(bool)
            });
    }
    
    static void Prefix(CardModel card, Creature? target)
    {
        MainFile.Tracker.OnAutoPlay(card, target);
    }
}