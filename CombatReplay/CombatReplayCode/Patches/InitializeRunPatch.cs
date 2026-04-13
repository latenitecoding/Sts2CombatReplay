using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(RunManager), "InitializeShared")]
public class InitializeRunPatch
{
    static void Postfix(INetGameService netService, long startTime)
    {
        MainFile.Tracker.OnInitializeRun(netService, startTime);
    }
}