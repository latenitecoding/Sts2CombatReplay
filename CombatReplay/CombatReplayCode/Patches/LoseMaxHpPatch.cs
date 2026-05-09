using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(CreatureCmd), "LoseMaxHp")]
public class LoseMaxHpPatch
{
    static void Prefix(Creature creature, Decimal amount)
    {
        MainFile.Tracker.OnCreatureLoseMaxHp(creature, amount);
    }
}