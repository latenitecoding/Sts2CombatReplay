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

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        // this covers all cases of transformed, created, and given cards
        // there could be other cards that trigger both OnCardAdded and this event
        // the following condition is here to catch those possible cases
        if (ReplayLogger.Matches(_logger?.PeekBufferType(), ReplayLogger.MsgType.CardAdded))
        {
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\",
                ReplayLogger.MsgType.CardAdded);
            _db.TotalCardsCreated++;
        }
        else if (ReplayLogger.Matches(_logger?.PeekBufferType(), ReplayLogger.MsgType.CardCreated))
        {
            // replace the card created output with this now that the pileType has been populated
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\",
                ReplayLogger.MsgType.CardCreated);
            _db.OnCardCreated(card.Title, true, card.Pile is { Type: PileType.Hand });
        }
        else if (ReplayLogger.Matches(_logger?.PeekBufferType() , ReplayLogger.MsgType.CardGiven))
        {
            // replace the card given output with this now that the pileType has been populated
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\",
                ReplayLogger.MsgType.CardGiven);
        }
        else
        {
            // covers all other cases for how cards can enter into combat, such as transform
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}` <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        // this event is always called when a card is drawn from the deck into hand
        // the event for OnCardAdded is also called in those cases and should be replaced with this event
        // do not double count the number of times are card is added to hand
        if (!ReplayLogger.Matches(_logger?.PeekBufferType() , ReplayLogger.MsgType.CardAdded)) _db.OnCardDrawn(card.Title);
        else _db.TotalCardsDrawn++;
        BufferIt(
            $"> {FormatPlayer(card.Owner)} **drew** {FormatCard(card)} <\\",
            ReplayLogger.MsgType.CardDrawn,
            overwrite: ReplayLogger.MsgType.CardAdded);
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // created and given cards, like statuses, will trigger this, but their pileType won't be populated yet
        // these events are being buffered to be replaced later in the AfterCardEnteredCombat (always called)
        if (addedByPlayer)
        {
            BufferIt($"> {FormatPlayer(owner)} **created** `{card.Title}` <\\", ReplayLogger.MsgType.CardCreated);
            return;
        }
        BufferIt($"> {FormatPlayer(owner)} **gained** `{card.Title}` <\\", ReplayLogger.MsgType.CardGiven);
    }

    public void OnCardAdded(Player owner, CardModel card, PileType pileType)
    {
        // there are several flows for this event:
        // - card added -> card drawn to hand (handled in AfterCardDrawn)
        // - card added because it was moved from a non-hand pile to a non-hand pile (ignore)
        // - card added because it was moved from a non-hand pile to a hand pile (handled here)
        // - card is created by the player and added to hand (handled in AfterCardEnteredCombat)
        // - status card is given by enemy and added to a card pile (handled in AfterCardEnteredCombat)
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // cards that are created by the player or given by an enemy will also trigger the AfterCardEnteredCombat event
        if (ReplayLogger.Matches(_logger?.PeekBufferType() , ReplayLogger.MsgType.CardCreated)) return;
        if (ReplayLogger.Matches(_logger?.PeekBufferType(), ReplayLogger.MsgType.CardGiven)) return;
        // only interested in logging cards that are added to hand to keep track of cards in hand
        if (pileType != PileType.Hand) return;
        // cards that are drawn to hand will trigger this event so we need to buffer this to replace it later
        // cards can be pulled from other piles and added to hand, which is the only case that should be logged here
        BufferIt($"> {FormatPlayer(card.Owner)} **pulled** {FormatCard(card)} **into** `{pileType.ToString()}` <\\", ReplayLogger.MsgType.CardAdded);
        _db.OnCardAddedToHand(card.Title);
    }
    
    public void OnExecuteCard(PlayCardAction action)
    {
        // unlike other events, this should be triggered for all players so that we can see what cards other players are playing
        var card = action.NetCombatCard.ToCardModel();
        WriteIt(action.Target != null
            ? $"> === {FormatPlayer(action.Player)} **played** `{card.Title}` **targeting** {FormatCreature(action.Target)} === <\\"
            : $"> === {FormatPlayer(action.Player)} **played** `{card.Title}` === <\\");
        if (!LocalContext.IsMe(card.Owner)) return;
        _db.OnExecuteCard(card.Title);
    }

    public void OnTransformCard(Player owner, CardModel original)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // transform card events are always followed by AfterCardEnteredCombat events
        // the AfterCardEnteredCombat events have the finalized card data
        // this event only has the name of the card being transformed
        // this is due to having to use prefix patches on async Tasks
        WriteIt($"> {FormatPlayer(owner)} **transformed** `{original.Title}` <\\");
    }

    public void OnUpgradeCard(CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner) || !_db.IsInCombat()) return;
        WriteIt(card.Pile != null 
            ? $"> {FormatPlayer(card.Owner)} **upgraded** {FormatCard(card)} **in** `{card.Pile.Type.ToString()}` <\\"
            : $"> {FormatPlayer(card.Owner)} **upgraded** {FormatCard(card)} <\\");
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