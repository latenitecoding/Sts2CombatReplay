using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(PlayerCmd), "GainGold")]
public class GainGoldPatch
{
    static void Prefix(Decimal amount, Player player, bool wasStolenBack = false)
    {
        MainFile.Tracker.OnGoldGained(player, (int)amount, wasStolenBack);
    }
}