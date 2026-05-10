using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class StunCreaturePatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CreatureCmd), "Stun",
            new Type[]
            {
                typeof(Creature),
                typeof(string)
            });
    }

    static void Postfix(Creature creature)
    {
        MainFile.Tracker.OnCreatureStunned(creature);
    }
}