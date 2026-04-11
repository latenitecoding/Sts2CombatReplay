using HarmonyLib;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(OrbModel), "Trigger")]
public class OrbPassivePatch
{
    static void Prefix(OrbModel __instance)
    {
        MainFile.Tracker.OnOrbPassive(__instance);
    }
}