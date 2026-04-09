namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public int TotalDamage { get; set; }
    public int TotalTrueDamage { get; set; }
    public int TotalBlockedDamage { get; set; }
    
    public int TotalBlockGained { get; set; }
    public int TotalDamageReceived { get; set; }
    public int TotalTrueDamageReceived { get; set; }
    public int TotalBlockedDamageReceived { get; set; }
    
    public int TotalSelfDamage { get; set; }
    
    private int _currentTurnDamage;
    private int _currentTurnBlock;
    public int BestSingleTurnDamage { get; set; }
    public int BestSingleTurnBlock { get; set; }
    
    public void AddCombatDamageDealt(int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamage += totalDamage;
        _currentTurnDamage += totalDamage;
        _currentCombat.TotalDamageDealt += totalDamage;
        if (trueDamage.HasValue)
        {
            TotalTrueDamage += trueDamage.Value;
            _currentCombat.TotalTrueDamageDealt += trueDamage.Value;
        }

        if (!blockedDamage.HasValue) return;
        
        TotalBlockedDamage += blockedDamage.Value;
        _currentCombat.TotalBlockedDamageDealt += blockedDamage.Value;
    }

    public void AddCombatDamageReceived(int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamageReceived += totalDamage;
        _currentCombat.TotalDamageReceived += totalDamage;
        if (trueDamage.HasValue)
        {
            TotalTrueDamageReceived += trueDamage.Value;
            _currentCombat.TotalTrueDamageReceived += trueDamage.Value;
        }

        if (!blockedDamage.HasValue) return;
        
        TotalBlockedDamageReceived += blockedDamage.Value;
        _currentCombat.TotalBlockedDamageReceived += blockedDamage.Value;
    }

    public void AddCombatBlockGained(int amount)
    {
        TotalBlockGained += amount;
        _currentCombat.TotalBlockGained += amount;
        _currentTurnBlock += amount;
    }
}