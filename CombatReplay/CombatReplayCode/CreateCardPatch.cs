using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode;

[HarmonyPatch]
public class CreateCardPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardPileCmd), "AddGeneratedCardToCombat",
            new Type[]
            {
                typeof(CardModel),
                typeof(PileType),
                typeof(bool),
                typeof(CardPilePosition)
            });
    }

    static void Prefix(CardModel card, bool addedByPlayer)
    {
        MainFile.Tracker.RecordCardCreated(card.Owner, card, addedByPlayer);
    }
}