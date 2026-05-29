namespace CombatReplay.CombatReplayCode.ReplayDb;

public class CardStats
{
    public required string ModelId { get; set; }
    
    public int TimesPlayedByPlayer { get; set; }
    public int TimesAutoPlayed { get; set; }
    
    public int TimesAddedToHand { get; set; }
    public int TimesDiscarded { get; set; }
    
    public bool IsPower { get; init; }
    public bool IsUnplayable { get; init; }
    
    public Decimal PlayedByPlayerRatio { get; set; }
    
    public int TotalDamageDealt { get; set; }
    public int TotalBlockGained { get; set; }
    public int TotalSelfDamageDealt { get; set; }
}