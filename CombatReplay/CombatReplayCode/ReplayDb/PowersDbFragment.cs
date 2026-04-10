using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int TotalPowersAppliedToSelf { get; set; }
    
    public int TotalStrengthGained { get; set; }
    public int TotalVulnerableApplied { get; set; }
    public int TotalWeakApplied { get; set; }
    public int TotalPoisonApplied { get; set; }
    public int TotalDoomApplied { get; set; }
    
    public void OnApplyPower(PowerModel power, Creature target, Decimal amount, Creature? applier, CardModel? cardSource)
    {
        if (amount <= 0) return;
        
        var isMyCard = LocalContext.IsMine(cardSource);
        var imTargeted = LocalContext.IsMe(target) || (target is { IsPet: true }  && LocalContext.IsMe(target.PetOwner));
        var iApplied = LocalContext.IsMe(applier) || isMyCard;
        
        if (power.Title.GetFormattedText().Contains("Strength") && (imTargeted || isMyCard))
        {
            TotalStrengthGained += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Vulnerable") && target.IsEnemy && iApplied)
        {
            TotalVulnerableApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Weak") && target.IsEnemy && iApplied)
        {
            TotalWeakApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Poison") && target.IsEnemy && iApplied)
        {
            TotalPoisonApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Doom") && iApplied)
        {
            TotalDoomApplied += (int) amount;
        }

        if (imTargeted && isMyCard)
        {
            TotalPowersAppliedToSelf += 1;
        }
    }
}