using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Saves;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch(typeof(PlayerCombatState), "GainEnergy")]
public class PlayerEnergyGainPatch
{
    static void Postfix(Decimal amount)
    {
        MainFile.Tracker.OnEnergyGained(amount);
    }
}