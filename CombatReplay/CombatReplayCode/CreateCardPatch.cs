using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch]
public class CreateCardPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CombatState), "CreateCard",
            new Type[]
            {
                typeof(CardModel),
                typeof(Player)
            });
    }

    static void Prefix(CardModel canonicalCard, Player owner)
    {
        MainFile.Tracker.RecordCardCreated(owner, canonicalCard);
    }
}