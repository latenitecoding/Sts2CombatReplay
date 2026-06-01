using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Gold;
using MegaCrit.Sts2.Core.Entities.Players;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(PlayerCmd), "LoseGold")]
public class LostGoldPatch
{
    static void Prefix(Decimal amount, Player player, GoldLossType goldLossType = GoldLossType.Lost)
    {
        MainFile.Tracker.OnGoldLost(player, (int)amount, goldLossType);
    }
}