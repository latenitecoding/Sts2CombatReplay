using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class AddCardToPilePatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardPileCmd), "Add",
            new Type[]
            {
                typeof(IEnumerable<CardModel>),
                typeof(CardPile),
                typeof(CardPilePosition),
                typeof(AbstractModel),
                typeof(bool) 
            });
    }
    
    static void Prefix(IEnumerable<CardModel> cards, CardPile newPile)
    {
        foreach (var card in cards)
        {
            MainFile.Tracker.OnCardAdded(card.Owner, card, newPile.Type);
        }
    }
}