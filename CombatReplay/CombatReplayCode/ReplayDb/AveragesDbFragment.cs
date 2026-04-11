namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public Decimal AvgDamagePerTurn { get; set; }
    public Decimal AvgBlockPerTurn { get; set; }
    
    public Decimal AvgDamagePerCombat { get; set; }
    public Decimal AvgBlockPerCombat { get; set; }
    public Decimal AvgDamageReceivedPerCombat { get; set; }
    public Decimal AvgTrueDamageReceivedPerCombat { get; set; }
    public Decimal AvgBlockedDamageReceivedPerCombat { get; set; }
    
    public Decimal AvgPowersAppliedtoSelfPerCombat { get; set; }
    
    public Decimal AvgStrengthGainedPerCombat { get; set; }
    public Decimal AvgVulnerableAppliedPerCombat { get; set; }
    public Decimal AvgWeakAppliedPerCombat { get; set; }
    public Decimal AvgPoisonAppliedPerCombat { get; set; }
    public Decimal AvgDoomAppliedPerCombat { get; set; }
    
    private void UpdateAverages()
    {
        if (TotalTurnsPlayed > 0)
        {
            AvgDamagePerTurn = Math.Round(((decimal)TotalDamage) / TotalTurnsPlayed, 2);
            AvgBlockPerTurn = Math.Round(((decimal)TotalBlockGained) / TotalTurnsPlayed, 2);
        }

        if (CurrentCombat <= 0) return;
        
        AvgDamagePerCombat = Math.Round(((decimal)TotalDamage) / CurrentCombat, 2);
        AvgBlockPerCombat = Math.Round(((decimal)TotalBlockGained) / CurrentCombat, 2);
        AvgDamageReceivedPerCombat = Math.Round(((decimal)TotalDamageReceived) / CurrentCombat, 2);
        AvgTrueDamageReceivedPerCombat = Math.Round(((decimal)TotalTrueDamageReceived) / CurrentCombat, 2);
        AvgBlockedDamageReceivedPerCombat = Math.Round(((decimal)TotalBlockedDamageReceived) / CurrentCombat, 2);
        
        AvgPowersAppliedtoSelfPerCombat = Math.Round(((decimal)TotalPowersAppliedToSelf) / CurrentCombat, 2);

        AvgStrengthGainedPerCombat = Math.Round(((decimal)TotalStrengthGained) / CurrentCombat, 2);
        AvgVulnerableAppliedPerCombat = Math.Round(((decimal)TotalVulnerableApplied) / CurrentCombat, 2);
        AvgWeakAppliedPerCombat = Math.Round(((decimal)TotalWeakApplied) / CurrentCombat, 2);
        AvgPoisonAppliedPerCombat = Math.Round(((decimal)TotalPoisonApplied) / CurrentCombat, 2);
        AvgDoomAppliedPerCombat = Math.Round(((decimal)TotalDoomApplied) / CurrentCombat, 2);
    }
}