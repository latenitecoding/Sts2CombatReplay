namespace CombatReplay.CombatReplayCode.ReplayDb;

public class CardStats
{
    public int TimesPlayed { get; set; }
    public int TimesAddedToHand { get; set; }
    public int TimesDiscarded { get; set; }
    
    public Decimal PlayToDrawRatio { get; set; }
    
    public int TotalDamageDealt { get; set; }
    public int TotalBlockGained { get; set; }
}