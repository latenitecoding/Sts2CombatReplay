using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(CombatManager), "AddCreature")]
public class AddCreaturePatch
{
    static void Postfix(Creature creature)
    {
        MainFile.Tracker.OnCreatureAdded(creature);
    }
}