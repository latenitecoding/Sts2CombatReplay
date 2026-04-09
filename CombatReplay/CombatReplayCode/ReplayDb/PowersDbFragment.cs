namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int TotalPowersPlayed { get; set; }
    
    public int TotalStrengthGained { get; set; }
    public int TotalVulnerableApplied { get; set; }
    public int TotalWeakApplied { get; set; }
    public int TotalPoisonApplied { get; set; }
    public int TotalDoomApplied { get; set; }
}