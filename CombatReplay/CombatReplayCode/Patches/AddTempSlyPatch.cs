using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch(typeof(CardCmd), "ApplySingleTurnSly")]
public class AddTempSlyPatch
{
    static void Prefix(CardModel card)
    {
        MainFile.Tracker.OnCardGainsKeywords(card, true, CardKeyword.Sly);
    }
}