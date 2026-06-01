namespace CombatReplay.CombatReplayCode.ReplayDb;

public class CombatStats
{
    public int CombatId { get; set; }
    public required List<string> Enemies { get; init; }
        
    public int TotalTurns { get; set; }
        
    public int TotalDamageDealt { get; set; }
    public int TotalTrueDamageDealt { get; set; }
    public int TotalBlockedDamageDealt { get; set; }
    public int TotalOverkillDamageDealt { get; set; }
    public int TotalPoisonDamageDealt { get; set; }
        
    public int TotalBlockGained { get; set; }
        
    public int TotalDamageReceived { get; set; }
    public int TotalTrueDamageReceived { get; set; }
    public int TotalBlockedDamageReceived { get; set; }
    
    public int TotalStrengthGained { get; set; }
    public int TotalVulnerableApplied { get; set; }
    public int TotalWeakApplied { get; set; }
    public int TotalPoisonApplied { get; set; }
    public int TotalDoomApplied { get; set; }
}