using HarmonyLib;
using MegaCrit.Sts2.Core.GameActions;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(PlayCardAction), "ExecuteAction")]
public class PlayCardPatch
{
   static void Prefix(PlayCardAction __instance)
   {
      MainFile.Tracker.OnExecuteCard(__instance);
   } 
}
