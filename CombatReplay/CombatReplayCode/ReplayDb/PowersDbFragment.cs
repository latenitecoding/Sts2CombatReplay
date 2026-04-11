using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;

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
        
        if (power is StrengthPower && (imTargeted || isMyCard))
        {
            TotalStrengthGained += (int) amount;
        }
        else if (power is VulnerablePower && target.IsEnemy && iApplied)
        {
            TotalVulnerableApplied += (int) amount;
        }
        else if (power is WeakPower && target.IsEnemy && iApplied)
        {
            TotalWeakApplied += (int) amount;
        }
        else if (power is PoisonPower && target.IsEnemy && iApplied)
        {
            TotalPoisonApplied += (int) amount;
        }
        else if (power is DoomPower && target.IsEnemy && iApplied)
        {
            TotalDoomApplied += (int) amount;
        }

        if (imTargeted && isMyCard) TotalPowersAppliedToSelf++;
    }
}