using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int CurrentTurn { get; set; }
    private bool _inCombat;
    
    public int TotalTurnsPlayed { get; set; }
    public int TotalEnemiesFought { get; set; }
    public int TotalPotionsUsed { get; set; }
    
    private CombatStats _currentCombat = new() { Enemies = [] };
    public CombatStats? HeroicCombat { get; set; }
    public CombatStats? NemesisCombat { get; set; }
    public List<CombatStats> Combats { get; init; } = [];

    private readonly List<Creature> _currentCreatures = [];
    
    public IReadOnlyList<Creature> GetCombatCreatureList() => _currentCreatures;
    
    public void StartCombat()
    {
        CurrentCombat += 1;
        CurrentTurn = 0;
        _inCombat = true;

        _currentCombat.CombatId = CurrentCombat;
    }
    
    public void NextTurn()
    {
        CurrentTurn += 1;
        _currentCombat.TotalTurns += 1;
        SetBestCardTurnStats();
    }

    public void EndCombat()
    {
        _inCombat = false;

        SetBestCardTurnStats();
        SetAverages();
        RecordCombat();

        _currentCreatures.Clear();
        _currentCombat = new CombatStats() { Enemies = [] };
    }

    public bool IsInCombat() => _inCombat;
    
    public void RecordCombat()
    {
        Combats.Add(_currentCombat);
        if (HeroicCombat == null || _currentCombat.TotalDamageDealt > HeroicCombat.TotalDamageDealt)
        {
            HeroicCombat = _currentCombat;
        }
        if (NemesisCombat == null || _currentCombat.TotalTrueDamageReceived > NemesisCombat.TotalTrueDamageReceived)
        {
            NemesisCombat = _currentCombat;
        }
    }
    
    public void AddCombatCreature(Creature creature, string fmtTitle)
    {
        _currentCreatures.Add(creature);
        if (creature.IsEnemy)
        {
            _currentCombat.Enemies.Add(fmtTitle);
        }
    }

    private void SetBestTurnStats()
    {
        BestSingleTurnDamage = Math.Max(BestSingleTurnDamage, _currentTurnDamage);
        BestSingleTurnBlock = Math.Max(BestSingleTurnBlock, _currentTurnBlock);
        
        _currentTurnDamage = 0;
        _currentTurnBlock = 0;
        
        SetBestCardTurnStats();
    }
}