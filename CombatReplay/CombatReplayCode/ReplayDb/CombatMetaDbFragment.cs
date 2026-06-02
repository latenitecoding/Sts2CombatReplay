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
    public List<CombatStats> HeroicCombatByAct { get; set; } = [];
    public List<CombatStats> NemesisCombatByAct { get; set; } = [];
    public List<CombatStats> Combats { get; init; } = [];

    public bool IsInCombat() => _inCombat;
    
    public void OnEndCombat()
    {
        _inCombat = false;
        
        UpdateTurnStats();
        UpdateAverages();
        
        Combats.Add(_currentCombat);

        var currentAct = Math.Max(FinalAct, 1);
        
        if (HeroicCombatByAct.Count < currentAct)
        {
            HeroicCombatByAct.Add(_currentCombat);
        }
        else if (_currentCombat.TotalDamageDealt > HeroicCombatByAct[currentAct].TotalDamageDealt)
        {
            HeroicCombatByAct[currentAct] = _currentCombat;
        }

        if (NemesisCombatByAct.Count < currentAct)
        {
            NemesisCombatByAct.Add(_currentCombat);
        }
        else if (_currentCombat.TotalTrueDamageReceived > NemesisCombatByAct[currentAct].TotalTrueDamageReceived)
        {
            NemesisCombatByAct[currentAct] = _currentCombat;
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
        FinalCombat += 1;
        CurrentTurn = 0;
        _inCombat = true;

        _currentCombat.CombatId = FinalCombat;
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