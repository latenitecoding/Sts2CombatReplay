using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int TotalPetDamage { get; set; }
    public int TotalPetDamageReceived { get; set; }
    
    public int TotalRelicPowerOrbDamage { get; set; }
    public int TotalRelicPowerOrbBlock { get; set; }
    
    public int TotalDamage { get; set; }
    public int TotalTrueDamage { get; set; }
    public int TotalBlockedDamage { get; set; }
    
    public int TotalBlockGained { get; set; }
    public int TotalDamageReceived { get; set; }
    public int TotalTrueDamageReceived { get; set; }
    public int TotalBlockedDamageReceived { get; set; }
    public int TotalOverkillDamage { get; set; }
    
    public int TotalSelfDamage { get; set; }
    
    private int _currentTurnDamage;
    private int _currentTurnBlock;
    public int BestSingleTurnDamage { get; set; }
    public int BestSingleTurnBlock { get; set; }
    
    public void AddCombatDamageDealt(Creature? dealer, Creature target, int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamage += totalDamage;
        _currentTurnDamage += totalDamage;
        _currentCombat.TotalDamageDealt += totalDamage;
        
        TotalTrueDamage += trueDamage ?? 0;
        _currentCombat.TotalTrueDamageDealt += trueDamage ?? 0;

        TotalBlockedDamage += blockedDamage ?? 0;
        _currentCombat.TotalBlockedDamageDealt += blockedDamage ?? 0;

        TotalOverkillDamage += totalDamage - (trueDamage ?? 0) - (blockedDamage ?? 0);
    }

    public void AddCombatDamageReceived(int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamageReceived += totalDamage;
        _currentCombat.TotalDamageReceived += totalDamage;
        
        TotalTrueDamageReceived += trueDamage ?? 0;
        _currentCombat.TotalTrueDamageReceived += trueDamage ?? 0;

        TotalBlockedDamageReceived += blockedDamage ?? 0;
        _currentCombat.TotalBlockedDamageReceived += blockedDamage ?? 0;
    }

    public void AddCombatBlockGained(CardModel? cardSource, int amount)
    {
        TotalBlockGained += amount;
        _currentCombat.TotalBlockGained += amount;
        _currentTurnBlock += amount;
        
        if (cardSource != null)
        {
            AddBlockGainedByCard(cardSource, amount);
        }
        else
        {
            TotalRelicPowerOrbBlock += amount;
        }
    }

    public void OnCombatDamageDealt(Creature? dealer, Creature target, CardModel? cardSource, int totalDamage, int? trueDamage,
        int? blockedDamage)
    {
        if (target.IsEnemy && (LocalContext.IsMe(dealer) || (dealer is { IsPet: true } && LocalContext.IsMe(dealer.PetOwner))))
        {
            AddCombatDamageDealt(dealer, target, totalDamage, trueDamage, blockedDamage);
            if (dealer is { IsPet: true }) TotalPetDamage += totalDamage;
        }
        else if (LocalContext.IsMe(target) || (target is { IsPet: true } && LocalContext.IsMe(target.PetOwner)))
        {
            if (target is { IsPet: true }) TotalPetDamageReceived += totalDamage;
            else
            {
                AddCombatDamageReceived(totalDamage, trueDamage, blockedDamage);
                if (dealer != null && LocalContext.IsMe(dealer.Player))
                {
                    TotalSelfDamage += totalDamage;
                }
            }

            if (cardSource != null && LocalContext.IsMe(cardSource.Owner))
            {
                AddDamageDealtByCard(cardSource, totalDamage, isSelfDamage: true);
            }

            return;
        }       
        
        if (cardSource != null && LocalContext.IsMe(cardSource.Owner))
        {
            AddDamageDealtByCard(cardSource, totalDamage, isSelfDamage: false);
        }
        else if (cardSource == null && dealer != null && LocalContext.IsMe(dealer.Player))
        {
            TotalRelicPowerOrbDamage += totalDamage;
            AddCombatDamageDealt(dealer, target, totalDamage, trueDamage, blockedDamage);
        }
    }
}