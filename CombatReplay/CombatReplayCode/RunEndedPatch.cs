using HarmonyLib;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch(typeof(RunManager), "OnEnded")]
public class RunEndedPatch
{
    static void Postfix(SerializableRun __result)
    {
        MainFile.Tracker.OnRunEnded(__result.StartTime);
    }
}