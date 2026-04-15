using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Nodes.CommonUi;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class UpgradeCardPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardCmd), "Upgrade",
            new Type[]
            {
                typeof(IEnumerable<CardModel>),
                typeof(CardPreviewStyle)
            });
    }

    static void Postfix(IEnumerable<CardModel> cards)
    {
        foreach (var card in cards)
        {
            MainFile.Tracker.OnUpgradeCard(card);
        }
    }}