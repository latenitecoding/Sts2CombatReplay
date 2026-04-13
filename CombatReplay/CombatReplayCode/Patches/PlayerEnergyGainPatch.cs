using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(PlayerCombatState), "GainEnergy")]
public class PlayerEnergyGainPatch
{
    static void Postfix(PlayerCombatState __instance, Decimal amount)
    {
        // I believe this only fires for the local player
        MainFile.Tracker.OnGainEnergy(__instance, amount);
    }
}