using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class ApplyPowerPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(PowerCmd), "Apply",
            new Type[]
            {
                typeof(PlayerChoiceContext),
                typeof(PowerModel),
                typeof(Creature),
                typeof(Decimal),
                typeof(Creature),
                typeof(CardModel),
                typeof(bool)
            });
    }

    static void Prefix(PowerModel power, Creature target, Decimal amount, Creature? applier, CardModel? cardSource)
    {
        MainFile.Tracker.OnApplyPower(power, target, amount, applier, cardSource);
    }
}