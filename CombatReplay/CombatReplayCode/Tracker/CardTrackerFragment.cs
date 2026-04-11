using CombatReplay.CombatReplayCode.Utils;
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
    private readonly HashSet<CardModel> _givenCards = [];

    private void ClearTrackedCreatedCards()
    {
        _addedCards.Clear();
        _createdCards.Clear();
        _givenCards.Clear();
    }

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (_addedCards.Contains(card)) return Task.CompletedTask;
        // if this comes after a card added, then that event already populated the message correctly
        if (_logger?.GetBufferType() == ReplayLogger.MsgType.CardAdded)
        {
            _logger?.Flush();
            return Task.CompletedTask;
        }
        if (_logger?.GetBufferType() == ReplayLogger.MsgType.CardCreated)
        {
            // replace the card created output with this now that the pileType has been populated
            BufferIt(
                $"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} (@ `{card.Pile?.Type.ToString() ?? "N/A"}`) <\\",
                ReplayLogger.MsgType.CardEntered,
                ReplayLogger.MsgType.CardCreated);
            _db.OnCardCreated(card.Title, true, card.Pile is { Type: PileType.Hand });
        } else if (_logger?.GetBufferType() == ReplayLogger.MsgType.CardGiven)
        {
            // replace the card given output with this now that the pileType has been populated
            BufferIt(
                $"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\",
                ReplayLogger.MsgType.CardEntered,
                ReplayLogger.MsgType.CardGiven);
        }
        else
        {
            // covers all other cases for how cards can enter into combat
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        // card draw events can come after card added events, but we don't want to double log card added to hand
        if (_logger?.GetBufferType() != ReplayLogger.MsgType.CardAdded) _db.OnCardDrawn(card.Title);
        else _db.TotalCardsDrawn++;
        // replace cards added to hand with card drawn to hand
        BufferIt(
            $"> {FormatPlayer(card.Owner)} **drew** {FormatCard(card)} **into** `Hand` <\\",
            ReplayLogger.MsgType.Draw,
            overwrite: ReplayLogger.MsgType.CardAdded);
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // created and given cards, like statuses, will trigger this, but their pileType won't be populated yet
        if (addedByPlayer)
        {
            BufferIt($"> I **created** `{card.Title}` <\\", ReplayLogger.MsgType.CardCreated);
            return;
        }
        BufferIt($"> I **gained** `{card.Title}` <\\", ReplayLogger.MsgType.CardGiven);
    }

    // OnCardAdded is generally called when cards are created or drawn
    public void OnCardAdded(Player owner, CardModel card, PileType pileType)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // created and given cards, like statuses, will trigger this, but we have other output for those events
        if (_logger?.GetBufferType() == ReplayLogger.MsgType.CardCreated) return;
        if (_logger?.GetBufferType() == ReplayLogger.MsgType.CardGiven) return;
        if (pileType != PileType.Hand) return;
        // log all non-created cards that are pulled into hand and expect to replace this with a draw event
        BufferIt($"> {FormatPlayer(card.Owner)} **pulled** {FormatCard(card)} **into** `{pileType.ToString()}` <\\", ReplayLogger.MsgType.CardAdded);
        _db.OnCardAddedToHand(card.Title);
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