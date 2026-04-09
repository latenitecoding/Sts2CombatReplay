using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(PlayerCombatState), "GainEnergy")]
public class PlayerEnergyGainPatch
{
    static void Postfix(Decimal amount)
    {
        MainFile.Tracker.OnEnergyGained(amount);
    }
}