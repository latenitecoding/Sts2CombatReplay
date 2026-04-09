using HarmonyLib;
using MegaCrit.Sts2.Core.Multiplayer.Game.Lobby;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(StartRunLobby), "BeginRunForAllPlayers")]
public class SeedPatch
{
    static void Postfix(string seed)
    {
        MainFile.Tracker.RecordSeed(seed);
    }
}