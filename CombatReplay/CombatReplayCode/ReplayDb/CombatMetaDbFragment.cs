namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int TotalPetDamage { get; set; }
    public int TotalPetDamageReceived { get; set; }
    
    public int TotalRelicPowerDamage { get; set; }
    public int TotalRelicPowerBlock { get; set; }
    
    public int TotalEnergyGained { get; set; }
    public int TotalStarsGained { get; set; }
    public int TotalEnergySpent { get; set; }
    public int TotalStarsSpent { get; set; }
    
    public int TotalCardsDrawn { get; set; }
    public int TotalCardsPlayed { get; set; }
    public int TotalCardsDiscarded { get; set; }
    public int TotalCardsRetained { get; set; }
    public int TotalCardsExhausted { get; set; }
    public int TotalEmptyHands { get; set; }
    public int TotalDeckShuffles { get; set; }
    
    public int TotalOstyRevives { get; set; }
    public int TotalForged { get; set; }
    public int TotalCardsCreated { get; set; }
    public int TotalSummoned { get; set; }
    public int TotalOrbsChanneled { get; set; }
    public int TotalOrbsEvoked { get; set; }
}