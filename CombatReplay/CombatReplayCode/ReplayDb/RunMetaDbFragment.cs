namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public string? RunSeed { get; set; }
    public bool IsMultiplayer { get; set; }
    public string? AscensionLevel { get; set; }
    
    public int FinalAct { get; set; }
    public int FinalRoom { get; set; }
    public int FinalCombat { get; set; }
    
    public int TotalPotionsDiscarded { get; set; }
    public int TotalHpHealed { get; set; }
    
    public void NextAct() => FinalAct += 1;
    public void NextRoom() => FinalRoom += 1;
}