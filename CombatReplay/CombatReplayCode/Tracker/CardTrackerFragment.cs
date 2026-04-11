using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    private readonly Dictionary<string, string> _specialDynamicVars = new()
    {
        ["Uppercut"] = "`Damage 13`, `Weak 1`, `Vulnerable 1`",
        ["Uppercut+"] = "`Damage 13`, `Weak 2`, `Vulnerable 2`",
    };

    private readonly HashSet<CardModel> _addedCards = [];
    private readonly HashSet<CardModel> _createdCards = [];
    private readonly HashSet<CardModel> _drawnCards = [];
    private readonly HashSet<CardModel> _givenCards = [];

    private void ClearTrackedCreatedCards()
    {
        _addedCards.Clear();
        _createdCards.Clear();
        _drawnCards.Clear();
        _givenCards.Clear();
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (_addedCards.Contains(card)) return Task.CompletedTask;
        if (_createdCards.Contains(card))
        {
            WriteIt($"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} (@ `{card.Pile?.Type.ToString() ?? "N/A"}`) <\\");
            _db.OnCardCreated(card.Title, true, card.Pile is { Type: PileType.Hand });
        } else if (_givenCards.Contains(card))
        {
            WriteIt($"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} (@ `{card.Pile?.Type.ToString() ?? "N/A"}`) <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _drawnCards.Add(card);
        WriteIt($"> {FormatPlayer(card.Owner)} **drew** {FormatCard(card)} <\\");
        _db.OnCardDrawn(card.Title);
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        if (addedByPlayer)
        {
            _createdCards.Add(card);
            return;
        }
        _givenCards.Add(card);
    }

    public void OnCardAdded(Player owner, CardModel card, PileType pileType)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        if (_createdCards.Contains(card) || _givenCards.Contains(card)) return;
        if (_drawnCards.Contains(card) || pileType != PileType.Hand) return;
        _addedCards.Add(card);
        WriteIt($"> {FormatPlayer(card.Owner)} **added** {FormatCard(card)} (@ `{pileType.ToString()}`) <\\");
        _db.OnCardAdded(card.Title, true);
    }
    
    public void OnExecuteCard(PlayCardAction action)
    {
        ClearTrackedCreatedCards();
        var card = action.NetCombatCard.ToCardModel();
        WriteIt(action.Target != null
            ? $"> _{FormatPlayer(action.Player)} **played** `{card.Title}` **targeting** {FormatCreature(action.Target)}_ <\\"
            : $"> _{FormatPlayer(action.Player)} **played** `{card.Title}`_ <\\");
        if (!LocalContext.IsMe(card.Owner)) return;
        _db.OnExecuteCard(card.Title);
    }

    public void OnTransformCard(Player owner, CardModel original)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        WriteIt($"> {FormatPlayer(owner)} **transformed** `{original.Title}` <\\");
    }
    
    private string FormatCard(CardModel card)
    {
        var keywords = string.Join(", ", card.Keywords.Select(keyword => $"`{keyword}`"));
        var tags = string.Join(", ", card.Tags.Select(tag => $"`{tag}`"));

        var dynamicVars = (_specialDynamicVars.TryGetValue(card.Title, out var value))
            ? value
            : string.Join(", ", card.DynamicVars.Values.Select(dynamicVar => $"`{dynamicVar.Name.Replace("Power", "")} {(int) dynamicVar.EnchantedValue}`"));
        
        var enchantment = (card.Enchantment != null) ? $"`{card.Enchantment.Title.GetFormattedText()}`" : "";
        var affliction = (card.Affliction != null) ? $"`{card.Affliction.Title.GetFormattedText()}`" : "";

        var replayCount = card.GetEnchantedReplayCount();
        var replayEntry = (replayCount > 0) ? $"[Replay: `{replayCount}`]" : "";
        
        var energyCost = (card.EnergyCost.CostsX) ? "X" : card.EnergyCost.Canonical.ToString();
        var starCost = (card.HasStarCostX) ? "X" : card.CurrentStarCost.ToString();

        return card.CurrentStarCost > 0 || card.HasStarCostX
            ? $"`{card.Title}` [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy **and** `{starCost}` stars"
            : $"`{card.Title}` [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy";
    }
}