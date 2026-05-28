using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public Dictionary<string, CardStats> CardPlayStats { get; init; } = [];
    
    private string _prevCardPlay = "";
    private int _currentAttackDamage;
    private int _currentDefenseBlock;

    public BestOfStat? BestSingleDamage { get; set; }

    public BestOfStat? BestSingleBlock { get; set; }

    public Dictionary<string, CardStats> BestAttack { get; init; } = [];
    public Dictionary<string, CardStats> BestDefend { get; init; } = [];
    public Dictionary<string, CardStats> MostPlayedCard { get; init; } = [];
    public Dictionary<string, CardStats> MostLikedCard { get; init; } = [];
    public Dictionary<string, CardStats> MostIgnoredCard { get; init; } = [];
     
    public void AddBlockGainedByCard(CardModel card, int amount)
    {
        var (cardStats, cardTitle) = GetOrCreateCardStats(card);
        
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

    public void AddDamageDealtByCard(CardModel card, int amount, bool isSelfDamage = false)
    {
        var (cardStats, cardTitle) = GetOrCreateCardStats(card);

        if (isSelfDamage)
        {
            cardStats.TotalSelfDamageDealt += amount;
            return;
        }
        
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

    public void OnCardAddedToHand(CardModel card)
    {
        var (cardStats, _) = GetOrCreateCardStats(card);
        cardStats.TimesAddedToHand++;
        UpdatePlayFromHandRatio(cardStats);
    }

    public void OnCardCreated(CardModel card, bool addedByPlayer, bool addedToHand)
    {
        if (addedToHand) OnCardAddedToHand(card);
        if (addedByPlayer) TotalCardsCreated++;
    }

    public void OnCardDiscarded(CardModel card)
    {
        var (cardStats, _) = GetOrCreateCardStats(card);
        cardStats.TimesDiscarded++;
        TotalCardsDiscarded++;
    }

    public void OnCardDrawn(CardModel card)
    {
        TotalCardsDrawn++;
        OnCardAddedToHand(card);
    }

    public void OnExecuteCard(CardModel card)
    {
        var (cardStats, cardTitle) = GetOrCreateCardStats(card);
        
        cardStats.TimesPlayed++;
        TotalCardsPlayed++;
        
        if (MostPlayedCard.Count == 0 || cardStats.TimesPlayed > MostPlayedCard.Values.First().TimesPlayed)
        {
            MostPlayedCard.Clear();
            MostPlayedCard[TitleToKey(cardTitle)] = cardStats;
        }

        UpdatePlayFromHandRatio(cardStats);
       
        UpdateCardStats();
        _prevCardPlay = cardTitle;
    }

    private (CardStats, string) GetOrCreateCardStats(CardModel card)
    {
        var cardTitle = TitleToKey(card.Title);
        if (CardPlayStats.TryGetValue(cardTitle, out var stats)) return (stats, card.Title);
        
        stats = new CardStats()
        {
            ModelId = card.Id.ToString(),
            IsUnplayable = card.Keywords.Any(keyword => keyword == CardKeyword.Unplayable),
        };
        
        CardPlayStats[cardTitle] = stats;
        
        return (stats, card.Title);
    }
    
    private void UpdateCardStats()
    {
        if (BestSingleDamage is null)
        {
            BestSingleDamage = new BestOfStat()
            {
                Title = _prevCardPlay,
                Amount = _currentAttackDamage,
            };
        }
        else if (_currentAttackDamage > BestSingleDamage.Amount)
        {
            BestSingleDamage.Title = _prevCardPlay;
            BestSingleDamage.Amount = _currentAttackDamage;
        }

        if (BestSingleBlock is null)
        {
            BestSingleBlock = new BestOfStat()
            {
                Title = _prevCardPlay,
                Amount = _currentDefenseBlock,
            };
        }
        else if (_currentDefenseBlock > BestSingleBlock.Amount)
        {
            BestSingleBlock.Title = _prevCardPlay;
            BestSingleBlock.Amount = _currentDefenseBlock;
        }

        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
    }

    private void UpdateMostLikedIgnoredCards()
    {
        if (CardPlayStats.Count == 0) return;

        var validCards = CardPlayStats.Keys
            .Select(TitleToKey)
            .Where(key => CardPlayStats[key] is { TimesPlayed: > 1, IsUnplayable: false })
            .ToList();
        
        if (validCards.Count == 0) return;
        
        var avgCardPlays = validCards.Average(key => CardPlayStats[key].TimesPlayed);

        var bestPlayedCardKey = validCards.First(key => CardPlayStats[key].TimesPlayed >= avgCardPlays);
        var bestPlayedCard = CardPlayStats[bestPlayedCardKey];
        
        foreach (var key in validCards)
        {
            var card =  CardPlayStats[key];
            
            if (card.TimesPlayed < avgCardPlays) continue;
            if (card.PlayFromHandRatio < bestPlayedCard.PlayFromHandRatio) continue;
            if (card.PlayFromHandRatio == bestPlayedCard.PlayFromHandRatio &&
                card.TimesPlayed <= bestPlayedCard.TimesPlayed) continue;

            bestPlayedCardKey = key;
            bestPlayedCard = card;
        }
        
        MostLikedCard.Clear();
        MostLikedCard[bestPlayedCardKey] = bestPlayedCard;
        
        var lowerAverageCardPlays = validCards
            .Where(key => CardPlayStats[key].TimesPlayed <= avgCardPlays)
            .Average(key => CardPlayStats[key].TimesPlayed);
       
        var worstPlayedCardKey = validCards
            .First(key => lowerAverageCardPlays <= CardPlayStats[key].TimesPlayed && CardPlayStats[key].TimesPlayed <= avgCardPlays);
        var worstPlayedCard = CardPlayStats[worstPlayedCardKey];
        
        foreach (var key in validCards)
        {
            var card =  CardPlayStats[key];
            
            if (card.TimesPlayed < lowerAverageCardPlays || card.TimesPlayed > avgCardPlays) continue;
            if (card.PlayFromHandRatio > worstPlayedCard.PlayFromHandRatio) continue;
            if (card.PlayFromHandRatio == worstPlayedCard.PlayFromHandRatio &&
                card.TimesPlayed >= worstPlayedCard.TimesPlayed) continue;

            worstPlayedCardKey = key;
            worstPlayedCard = card;
        }

        MostIgnoredCard.Clear();
        MostIgnoredCard[worstPlayedCardKey] = worstPlayedCard;
    }

    private void UpdatePlayFromHandRatio(CardStats cardStats)
    {
        if (cardStats.TimesAddedToHand > 0)
        {
            cardStats.PlayFromHandRatio = Math.Round((decimal)cardStats.TimesPlayed / cardStats.TimesAddedToHand, 2);
        }
    }

    private static readonly Regex TitleToKeyRegex = new(@"\+\d*$", RegexOptions.Compiled);
    
    private static string TitleToKey(string cardTitle)
    {
        return TitleToKeyRegex.Replace(cardTitle, "");
    }
}