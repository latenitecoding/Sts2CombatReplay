using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class RemovePowerPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(PowerCmd), "Remove",
            new Type[]
            {
                typeof(PowerModel),
            });
    }

    static void Prefix(PowerModel? power)
    {
        MainFile.Tracker.OnRemovePower(power);
    }
}