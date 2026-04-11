using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(CombatManager), "AddCreature")]
public class AddCreaturePatch
{
    static void Postfix(Creature creature)
    {
        // only apparent way to grab creatures as they enter combat since there appears to be no hooks for it
        MainFile.Tracker.OnAddCreature(creature);
    }
}