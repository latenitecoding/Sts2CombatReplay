using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(CardCmd), "ApplyKeyword")]
public class AddKeywordsPatch
{
    static void Prefix(CardModel card, params CardKeyword[] keywords)
    {
        MainFile.Tracker.OnCardGainsKeywords(card, false, keywords);
    }
}