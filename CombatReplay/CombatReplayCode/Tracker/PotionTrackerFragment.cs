using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (!LocalContext.IsMe(potion.Owner)) return Task.CompletedTask;
        WriteIt($"> =={FormatPlayer(potion.Owner)} **discarded** {FormatPotion(potion)}==");
        _db.TotalPotionsDiscarded++;
        return Task.CompletedTask;
    }
    
    public override Task BeforePotionUsed(PotionModel potion, Creature? target)
    {
        // unlike other events, this should be triggered for all players so that we can see what potions other players are using
        WriteIt(target != null
            ? $"> =={FormatPlayer(potion.Owner)} **threw** {FormatPotion(potion)} **at** {FormatCreature(target)}=="
            : $"> =={FormatPlayer(potion.Owner)} **threw** {FormatPotion(potion)}==");

        if (LocalContext.IsMe(potion.Owner)) _db.TotalPotionsUsed++;
        return Task.CompletedTask;
    }
    
    public override bool ShouldProcurePotion(PotionModel potion, Player player)
    {
        // always return true so that we don't change core game logic
        if (player.Potions.Count() >= player.MaxPotionCount) return true;
        
        WriteIt($"> {FormatPlayer(player)} **procured** {FormatPotion(potion)}");
        
        return true;
    }
    
    private static string FormatPotion(PotionModel potion)
    {
        var dynamicVars = string.Join(", ", potion.DynamicVars.Values.Select(dynamicVar => $"`{dynamicVar.Name.Replace("Power", "")} {(int) dynamicVar.EnchantedValue}`"));
        return $"`{potion.Title.GetFormattedText()}` [{dynamicVars}]";
    }
}