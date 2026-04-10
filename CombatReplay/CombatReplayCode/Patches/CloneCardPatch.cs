using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class CloneCardPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CombatState), "CloneCard",
            new Type[]
            {
                typeof(CardModel),
            });
    }

    static void Prefix(CardModel mutableCard)
    {
        MainFile.Tracker.OnAddGeneratedCard(mutableCard.Owner, mutableCard);
    }
}