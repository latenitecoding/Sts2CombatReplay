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
        var (cardStats, cardTitle) = GetOrCreateCardStats(card);
        cardStats.TimesAddedToHand++;
        UpdatePlayFromHandRatio(cardTitle, cardStats);
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

        UpdatePlayFromHandRatio(cardTitle, cardStats);
       
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

    private void UpdatePlayFromHandRatio(string cardTitle, CardStats cardStats)
    {
        if (cardStats.TimesAddedToHand > 0)
        {
            cardStats.PlayFromHandRatio = Math.Round((decimal)cardStats.TimesPlayed / cardStats.TimesAddedToHand, 2);
        }

        if (cardStats.IsUnplayable) return;

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