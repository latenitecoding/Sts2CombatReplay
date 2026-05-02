namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int BonusEnergyGained { get; set; }
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
    public int CurrentTurn { get; set; }
    private bool _inCombat;
    
    public int TotalTurnsPlayed { get; set; }
    public int TotalPotionsUsed { get; set; }
    
    private CombatStats _currentCombat = new() { Enemies = [] };
    public CombatStats? HeroicCombat { get; set; }
    public CombatStats? NemesisCombat { get; set; }
    public List<CombatStats> Combats { get; init; } = [];

    public bool IsInCombat() => _inCombat;
    
    public void OnEndCombat()
    {
        _inCombat = false;
        
        UpdateTurnStats();
        UpdateAverages();
        
        Combats.Add(_currentCombat);
        if (HeroicCombat == null || _currentCombat.TotalDamageDealt > HeroicCombat.TotalDamageDealt)
        {
            HeroicCombat = _currentCombat;
        }
        if (NemesisCombat == null || _currentCombat.TotalTrueDamageReceived > NemesisCombat.TotalTrueDamageReceived)
        {
            NemesisCombat = _currentCombat;
        }
        
        _currentCreatures.Clear();
        _currentCombat = new CombatStats() { Enemies = [] };
        _prevCardPlay = "";
    }   
   
    public void OnNextTurn()
    {
        CurrentTurn += 1;
        _currentCombat.TotalTurns += 1;
        TotalTurnsPlayed += 1;
        UpdateTurnStats();
    }

    public void OnStartCombat()
    {
        CurrentCombat += 1;
        CurrentTurn = 0;
        _inCombat = true;

        _currentCombat.CombatId = CurrentCombat;
    }

    private void UpdateTurnStats()
    {
        BestSingleTurnDamage = Math.Max(BestSingleTurnDamage, _currentTurnDamage);
        BestSingleTurnBlock = Math.Max(BestSingleTurnBlock, _currentTurnBlock);
        
        _currentTurnDamage = 0;
        _currentTurnBlock = 0;

        UpdateCardStats();
    }
}