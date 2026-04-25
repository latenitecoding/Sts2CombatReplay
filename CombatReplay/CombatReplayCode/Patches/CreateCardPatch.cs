using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

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
                typeof(Player),
                typeof(CardPilePosition)
            });
    }

    static void Prefix(CardModel card, Player? creator)
    {
        MainFile.Tracker.OnAddGeneratedCard(card.Owner, card, creator);
    }
}