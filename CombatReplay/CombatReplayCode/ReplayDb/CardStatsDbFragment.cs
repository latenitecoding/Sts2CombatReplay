using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Models.Relics;

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
    public Dictionary<string, CardStats> MostLikedCard { get; init; } = [];
    public Dictionary<string, CardStats> MostIgnoredCard { get; init; } = [];
     
    public void AddBlockGainedByCard(string cardTitle, int amount)
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

    public void AddDamageDealtByCard(string cardTitle, int amount)
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

    public void OnCardAddedToHand(string cardTitle)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TimesAddedToHand++;
        UpdatePlayFromHandRatio(cardTitle, cardStats);
    }

    public void OnCardCreated(string cardTitle, bool addedByPlayer, bool addedToHand)
    {
        if (addedToHand) OnCardAddedToHand(cardTitle);
        if (addedByPlayer) TotalCardsCreated++;
    }

    public void OnCardDiscarded(string cardTitle)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TimesDiscarded++;
        TotalCardsDiscarded++;
    }

    public void OnCardDrawn(string cardTitle)
    {
        TotalCardsDrawn++;
        OnCardAddedToHand(cardTitle);
    }

    public void OnExecuteCard(string cardTitle)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TimesPlayed++;
        TotalCardsPlayed++;
        
        if (MostPlayedCard.Count == 0 || cardStats.TimesPlayed > MostPlayedCard.Values.First().TimesPlayed)
        {
            MostPlayedCard.Clear();
            MostPlayedCard[TitleToKey(cardTitle)] = cardStats;
        }

        UpdatePlayFromHandRatio(cardTitle, cardStats);
       
        UpdateCardStats();
        _prevCardPlay = cardTitle;
    }

    private CardStats GetOrCreateCardStats(string cardTitle)
    {
        cardTitle = TitleToKey(cardTitle);
        if (CardPlayStats.TryGetValue(cardTitle, out var stats)) return stats;
        stats = new CardStats();
        CardPlayStats[cardTitle] = stats;
        return stats;
    }
    
    private void UpdateCardStats()
    {
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);

        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
    }

    private void UpdatePlayFromHandRatio(string cardTitle, CardStats cardStats)
    {
        if (cardStats.TimesAddedToHand > 0)
        {
            cardStats.PlayFromHandRatio = Math.Round((decimal)cardStats.TimesPlayed / cardStats.TimesAddedToHand, 2);
        }

        if (MostLikedCard.Count == 0 || cardStats.PlayFromHandRatio > MostLikedCard.Values.First().PlayFromHandRatio)
        {
            MostLikedCard.Clear();
            MostLikedCard[TitleToKey(cardTitle)] = cardStats;
        }

        if (MostIgnoredCard.Count == 0 || cardStats.PlayFromHandRatio < MostIgnoredCard.Values.First().PlayFromHandRatio)
        {
            MostIgnoredCard.Clear();
            MostIgnoredCard[TitleToKey(cardTitle)] = cardStats;
        }   
    }

    private static readonly Regex TitleToKeyRegex = new(@"\+\d*$", RegexOptions.Compiled);
    
    private static string TitleToKey(string cardTitle)
    {
        return TitleToKeyRegex.Replace(cardTitle, "");
    }
}