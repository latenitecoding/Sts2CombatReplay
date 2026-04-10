using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Nodes.CommonUi;
using MegaCrit.Sts2.Core.Random;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class TransformCardPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(CardCmd), "Transform",
            new Type[]
            {
                typeof(IEnumerable<CardTransformation>),
                typeof(Rng),
                typeof(CardPreviewStyle)
            });
    }

    static void Prefix(IEnumerable<CardTransformation> transformations)
    {
        foreach (var transformation in transformations)
        {
            MainFile.Tracker.OnTransformCard(transformation.Original.Owner, transformation.Original, transformation.Replacement ?? transformation.Original);
        }
    }
}