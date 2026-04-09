using System.Reflection;
using HarmonyLib;
using MegaCrit.Sts2.Core.Commands;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Patches;

[HarmonyPatch]
public class ModifyPowerAmountPatch
{
    static MethodBase TargetMethod()
    {
        return AccessTools.Method(typeof(PowerCmd), "ModifyAmount",
            new Type[]
            {
                typeof(PowerModel),
                typeof(Decimal),
                typeof(Creature),
                typeof(CardModel),
                typeof(bool)
            });
    }
 
    static void Prefix(PowerModel power, Decimal offset, Creature? applier, CardModel? cardSource)
    {
        MainFile.Tracker.RecordPower(power, power.Owner, offset, applier, cardSource);
    }
}