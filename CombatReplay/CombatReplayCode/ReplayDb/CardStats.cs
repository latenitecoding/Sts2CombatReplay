namespace CombatReplay.CombatReplayCode.ReplayDb;

public class CardStats
{
    public string ModelId { get; set; }
    
    public int TimesPlayed { get; set; }
    public int TimesAddedToHand { get; set; }
    public int TimesDiscarded { get; set; }
    public bool IsUnplayable { get; init; }
    
    public Decimal PlayFromHandRatio { get; set; }
    
    public int TotalDamageDealt { get; set; }
    public int TotalBlockGained { get; set; }
    public int TotalSelfDamageDealt { get; set; }
}