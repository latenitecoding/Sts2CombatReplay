using System.Text.RegularExpressions;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public Dictionary<string, CardStats> CardPlayStats { get; init; } = new();
    
    private string _prevCardPlay = "";
    private int _currentAttackDamage;
    private int _currentDefenseBlock;
    
    public int BestSingleDamage { get; set; }
    public int BestSingleBlock { get; set; }

    public Dictionary<string, CardStats> BestAttack { get; init; } = [];
    public Dictionary<string, CardStats> BestDefend { get; init; } = [];
    public Dictionary<string, CardStats> MostPlayedCard { get; init; } = [];
    
    public void AddCardPlay(string cardTitle)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TimesPlayed++;

        if (MostPlayedCard.Count == 0 || cardStats.TimesPlayed > MostPlayedCard.Values.First().TimesPlayed)
        {
            MostPlayedCard.Clear();
            MostPlayedCard[TitleToKey(cardTitle)] = cardStats;
        }
        
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);
        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
        
        _prevCardPlay = cardTitle;
    }

    public void AddDamageDealt(string cardTitle, int amount)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TotalDamageDealt += amount;

        if (BestAttack.Count == 0 || cardStats.TotalDamageDealt > BestAttack.Values.First().TotalDamageDealt)
        {
            BestAttack.Clear();
            BestAttack[TitleToKey(cardTitle)] = cardStats;
        }

        if (cardTitle == _prevCardPlay)
        {
            _currentAttackDamage += amount;
        }
    }
    
    public void AddBlockGained(string cardTitle, int amount)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TotalBlockGained += amount;

        if (BestDefend.Count == 0 || cardStats.TotalBlockGained > BestDefend.Values.First().TotalBlockGained)
        {
            BestDefend.Clear();
            BestDefend[TitleToKey(cardTitle)] = cardStats;
        }

        if (cardTitle == _prevCardPlay)
        {
            _currentDefenseBlock += amount;
        }
    }

    private CardStats GetOrCreateCardStats(string cardTitle)
    {
        cardTitle = TitleToKey(cardTitle);
        if (CardPlayStats.TryGetValue(cardTitle, out var stats)) return stats;
        stats = new CardStats();
        CardPlayStats[cardTitle] = stats;
        return stats;
    }
    
    private void SetBestCardTurnStats()
    {
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);

        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
    }
    
    [GeneratedRegex(@"\+\d*$")]
    private static partial Regex TitleToKeyRegex();
    
    private static string TitleToKey(string cardTitle)
    {
        return TitleToKeyRegex().Replace(cardTitle, "").Trim();
    }
}