using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (!LocalContext.IsMe(potion.Owner)) return Task.CompletedTask;
        WriteIt($"> I **discarded** `{FormatPotion(potion)}` <\\");
        _db.TotalPotionsDiscarded++;
        return Task.CompletedTask;
    }
    
    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        WriteIt(target != null
            ? $"> {FormatPlayer(potion.Owner)} **used** `{FormatPotion(potion)}` **on** {FormatCreature(target)} <\\"
            : $"> {FormatPlayer(potion.Owner)} **used** `{FormatPotion(potion)}` <\\");
        if (LocalContext.IsMe(potion.Owner)) _db.TotalPotionsUsed++;
        return Task.CompletedTask;
    }

    private static string FormatPotion(PotionModel potion)
    {
        var dynamicVars = string.Join(", ", potion.DynamicVars.Values.Select(dynamicVar => $"`{dynamicVar.Name.Replace("Power", "")} {(int) dynamicVar.EnchantedValue}`"));
        return $"`{potion.Title.GetFormattedText()}` [{dynamicVars}]";
    }
}