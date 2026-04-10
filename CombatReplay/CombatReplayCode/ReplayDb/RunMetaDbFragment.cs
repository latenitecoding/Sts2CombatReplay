namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public string? RunSeed { get; set; }
    public bool IsMultiplayer { get; set; }
    public int CurrentAct { get; set; }
    public int CurrentRoom { get; set; }
    public int CurrentCombat { get; set; }
    
    public int TotalPotionsDiscarded { get; set; }
    public int TotalHpHealed { get; set; }
    
    public void NextAct() => CurrentAct += 1;
    public void NextRoom() => CurrentRoom += 1;
}