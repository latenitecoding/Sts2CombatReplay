namespace CombatReplay.CombatReplayCode.ReplayDb;

public class CombatStats
{
    public int CombatId { get; set; }
    public required List<string> Enemies { get; init; }
        
    public int TotalTurns { get; set; }
        
    public int TotalDamageDealt { get; set; }
    public int TotalTrueDamageDealt { get; set; }
    public int TotalBlockedDamageDealt { get; set; }
        
    public int TotalBlockGained { get; set; }
        
    public int TotalDamageReceived { get; set; }
    public int TotalTrueDamageReceived { get; set; }
    public int TotalBlockedDamageReceived { get; set; }
}