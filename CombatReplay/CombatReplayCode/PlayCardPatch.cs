using GodotPlugins.Game;
using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public class PlayCardPatch
{
   static void Prefix(PlayCardAction __instance)
   {
      MainFile.Tracker.RecordCardPlayed(__instance);
   } 
}
