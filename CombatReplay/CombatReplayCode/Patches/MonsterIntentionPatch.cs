using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Random;

namespace CombatReplay.CombatReplayCode.Patches;

[Harmony]
public class MonsterIntentionPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(MonsterMoveStateMachine), "RollMove",
            new Type[]
            {
                typeof(IEnumerable<Creature>),
                typeof(Creature),
                typeof(Rng)
            });
    }

    static void Postfix(Creature owner, MoveState __result)
    {
        MainFile.Tracker.RecordEnemyIntent(owner, __result);
    }
}