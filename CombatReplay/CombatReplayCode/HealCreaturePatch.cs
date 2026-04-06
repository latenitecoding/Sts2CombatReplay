using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch(typeof(CreatureCmd), "Heal")]
public class HealCreaturePatch
{
    static void Prefix(Creature creature, Decimal amount)
    {
        MainFile.Tracker.OnCreatureHeal(creature, amount);
    }
}