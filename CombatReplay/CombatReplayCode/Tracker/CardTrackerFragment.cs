using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Cards;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using System.Text;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    private readonly Dictionary<string, string> _specialDynamicVars = new()
    {
        ["Uppercut"] = "`Damage 13`, `Weak 1`, `Vulnerable 1`",
        ["Uppercut+"] = "`Damage 13`, `Weak 2`, `Vulnerable 2`"
    };

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        
        // this covers all cases of transformed, created, and given cards
        // there could be other cards that trigger both OnCardAdded and this event
        // the following condition is here to catch those possible cases
        if (CheckIt(ReplayLogger.MsgType.CardAdded))
        {
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}`",
                overwriting: ReplayLogger.MsgType.CardAdded);
            _db.TotalCardsCreated++;
        }
        else if (CheckIt(ReplayLogger.MsgType.CardCreated))
        {
            // replace the card created output with this now that the pileType has been populated
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **created** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}`",
                overwriting: ReplayLogger.MsgType.CardCreated);
            _db.OnCardCreated(card, true, card.Pile is { Type: PileType.Hand });
        }
        else if (CheckIt(ReplayLogger.MsgType.CardDrawn))
        {
            Flush();
        }
        else if (CheckIt(ReplayLogger.MsgType.CardGiven))
        {
            // replace the card given output with this now that the pileType has been populated
            WriteIt(
                $"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}`",
                overwriting: ReplayLogger.MsgType.CardGiven);
            
            if (card.Pile is { Type: PileType.Hand }) _db.OnCardAddedToHand(card);
        }
        else
        {
            // covers all other cases for how cards can enter into combat, such as transform
            WriteIt($"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} **into** `{card.Pile?.Type.ToString() ?? "N/A"}`");
            
            if (card.Pile is { Type: PileType.Hand }) _db.OnCardAddedToHand(card);
        }
        
        return Task.CompletedTask;
    }
    
    public override Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        
        WriteIt($"> {FormatPlayer(card.Owner)} **discarded** {FormatCard(card)}");
        _db.OnCardDiscarded(card);
        
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        
        // this event is always called when a card is drawn from the deck into hand
        // the event for OnCardAdded is also called in those cases and should be replaced with this event
        // do not double count the number of times are card is added to hand
        var (_, found) = BufferIt(
            $"> {FormatPlayer(card.Owner)} **drew** {FormatCard(card)}",
            ReplayLogger.MsgType.CardDrawn,
            overwriting: ReplayLogger.MsgType.CardAdded,
            expecting: ReplayLogger.MsgType.CardAdded);
        
        if (found)
        {
            _db.TotalCardsDrawn++;
            Flush();
            return Task.CompletedTask;
        }
        
        _db.OnCardDrawn(card);
        return Task.CompletedTask;
    }
    
    public override Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        
        _db.TotalCardsExhausted++;
        WriteIt($"> {FormatPlayer(card.Owner)} **exhausted** {FormatCard(card)}");

        _db.OnCardExhausted(card);
        
        return Task.CompletedTask;
    }

    public override Task AfterHandEmptied(PlayerChoiceContext ctx, Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        
        _db.TotalEmptyHands++;
        WriteIt($"> {FormatPlayer(player)} **emptied** `Hand`");
        
        return Task.CompletedTask;
    }
    
    public override Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)
    {
        if (!LocalContext.IsMe(shuffler)) return Task.CompletedTask;
        
        _db.TotalDeckShuffles++;
        ClearIt(ReplayLogger.MsgType.CardAdded);
        WriteIt($"> {FormatPlayer(shuffler)} **shuffled** `Discard`");
        
        return Task.CompletedTask;
    }

    public void OnAddGeneratedCard(Player owner, CardModel card, Player? creator)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        
        // created and given cards, like statuses, will trigger this, but their pileType won't be populated yet
        // these events are being buffered to be replaced later in the AfterCardEnteredCombat (always called)
        // some creation events will redundantly trigger this hook so guards are also necessary
        if (CheckIt(ReplayLogger.MsgType.CardCreated | ReplayLogger.MsgType.CardGiven)) return;
        
        if (LocalContext.IsMe(creator))
        {
            BufferIt(
                $"> {FormatPlayer(owner)} **created** {FormatCard(card)}",
                ReplayLogger.MsgType.CardCreated,
                overwriting: ReplayLogger.MsgType.None,
                expecting: ReplayLogger.MsgType.CardAdded | ReplayLogger.MsgType.CardEntering);
            return;
        }

        BufferIt(
            $"> {FormatPlayer(owner)} **gained** {FormatCard(card)}",
            ReplayLogger.MsgType.CardGiven,
            overwriting: ReplayLogger.MsgType.None,
            expecting: ReplayLogger.MsgType.CardAdded | ReplayLogger.MsgType.CardEntering);
    }
    
    public void OnAutoPlay(CardModel card, Creature? target)
    {
        var dealer = card.Owner;
        if (LocalContext.IsMe(dealer))
        {
            var (_, found) = BufferIt(target != null
                ? $"> =={FormatPlayer(dealer)} **auto-played** {FormatCard(card)} **targeting** {FormatCreature(target)}=="
                : $"> =={FormatPlayer(dealer)} **auto-played** {FormatCard(card)}==",
                ReplayLogger.MsgType.CardPlayed,
                overwriting: ReplayLogger.MsgType.CardPlayed,
                expecting: ReplayLogger.MsgType.CardAdded);
            
            if (!found) _db.OnExecuteCard(card, isAutoPlayed: true);
        }
        else
        {
            BufferIt(target != null
                ? $"> --{FormatPlayer(dealer)} **auto-played** {FormatCard(card)} **targeting** {FormatCreature(target)}--"
                : $"> --{FormatPlayer(dealer)} **auto-played** {FormatCard(card)}--",
                ReplayLogger.MsgType.CardPlayed,
                overwriting: ReplayLogger.MsgType.CardPlayed,
                expecting: ReplayLogger.MsgType.CardAdded);
        }
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
        if (CheckIt(ReplayLogger.MsgType.CardCreated | ReplayLogger.MsgType.CardGiven)) return;
        if (pileType is PileType.Discard or PileType.Exhaust) return;
        
        if (CheckIt(ReplayLogger.MsgType.CardDrawn) || (pileType is PileType.Play && CheckIt(ReplayLogger.MsgType.CardPlayed)))
        {
            Flush();
            return;
        }

        if (pileType is PileType.Play)
        {
            BufferIt(
                $"> =={FormatPlayer(owner)} **auto-played** {FormatCard(card)}==",
                ReplayLogger.MsgType.CardPlayed,
                overwriting: ReplayLogger.MsgType.None,
                expecting: ReplayLogger.MsgType.CardPlayed);
            
            _db.OnExecuteCard(card, isAutoPlayed: true);
            return;
        }

        // cards that are drawn to hand will trigger this event so we need to buffer this to replace it later
        // cards can be pulled from other piles and added to hand, which is the only case that should be logged here
        BufferIt(
            $"> {FormatPlayer(card.Owner)} **pulled** {FormatCard(card)} **into** `{pileType.ToString()}`",
            ReplayLogger.MsgType.CardAdded,
            overwriting: ReplayLogger.MsgType.None,
            expecting: ReplayLogger.MsgType.CardAdded | ReplayLogger.MsgType.CardDrawn | ReplayLogger.MsgType.CardEntering | ReplayLogger.MsgType.PlayerOrEnemyWasHit | ReplayLogger.MsgType.PowerApplied | ReplayLogger.MsgType.TookDamage);
        
        if (pileType is PileType.Hand) _db.OnCardAddedToHand(card);
    }

    public void OnCardGainsKeywords(CardModel card, bool isSingleTurn, params CardKeyword[] keywords)
    {
        var gainedKeywords = string.Join(", ", keywords.Select(keyword => $"`{keyword}`{(isSingleTurn ? " (1 Turn)" : "")}"));
        if (gainedKeywords.Length > 0) return;

        WriteIt($"> {FormatCard(card)} **gained** k[{gainedKeywords}]");
    }

    public void OnCardLosesKeywords(CardModel card, bool isSingleTurn, params CardKeyword[] keywords)
    {
        var lostKeywords = string.Join(", ", keywords.Select(keyword => $"`{keyword}`{(isSingleTurn ? " (1 Turn)" : "")}"));
        if (lostKeywords.Length > 0) return;

        WriteIt($"> {FormatCard(card)} **lost** k[{lostKeywords}]");
    }
    
    public void OnExecuteCard(PlayCardAction action)
    {
        // unlike other events, this should be triggered for all players so that we can see what cards other players are playing
        var card = action.NetCombatCard.ToCardModel();
        if (LocalContext.IsMe(card.Owner))
        {
            WriteIt(action.Target != null
                ? $"> =={FormatPlayer(action.Player)} **played** {FormatCard(card)} **targeting** {FormatCreature(action.Target)}=="
                : $"> =={FormatPlayer(action.Player)} **played** {FormatCard(card)}==");
        }
        else
        {
            WriteIt(action.Target != null
                ? $"> --{FormatPlayer(action.Player)} **played** {FormatCard(card)} **targeting** {FormatCreature(action.Target)}--"
                : $"> --{FormatPlayer(action.Player)} **played** {FormatCard(card)}--");
        }
        
        if (!LocalContext.IsMe(card.Owner)) return;
        _db.OnExecuteCard(card);
    }
   
    public void OnTransformCard(Player owner, CardModel original)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        // transform card events are always followed by AfterCardEnteredCombat events
        // the AfterCardEnteredCombat events have the finalized card data
        // this event only has the name of the card being transformed
        // this is due to having to use prefix patches on async Tasks
        WriteIt(original.Pile != null 
            ? $"> {FormatPlayer(owner)} **transformed** {FormatCard(original)} **in** `{original.Pile.Type.ToString()}`"
            : $"> {FormatPlayer(owner)} **transformed** {FormatCard(original)}");
        _db.TotalCardsCreated++;
    }

    public void OnUpgradeCard(CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner) || !_db.IsInCombat()) return;
        WriteIt(card.Pile != null 
            ? $"> {FormatPlayer(card.Owner)} **upgraded** {FormatCard(card)} **in** `{card.Pile.Type.ToString()}`"
            : $"> {FormatPlayer(card.Owner)} **upgraded** {FormatCard(card)}");
    }
    
    private string FormatCard(CardModel card) 
    {
        var sb = new StringBuilder($"`{card.Title}`");
        
        var dynamicVars = (_specialDynamicVars.TryGetValue(card.Title, out var value))
            ? value
            : string.Join(", ", card.DynamicVars.Values.Select(dynamicVar =>
            {
                var dynamicVarName = dynamicVar.Name.Replace("Power", "");
                if (string.IsNullOrEmpty(dynamicVarName) && card.Title.IndexOf('+') is var plusIdx)
                {
                    dynamicVarName = plusIdx >= 0 ? card.Title[..plusIdx] : card.Title;
                }
                return $"`{dynamicVarName} {(int)dynamicVar.EnchantedValue}`";
            }));
        if (dynamicVars.Length > 0) sb.Append($" v[{dynamicVars}]");

        var tags = string.Join(", ", card.Tags.Select(tag => $"`{tag}`"));
        if (tags.Length > 0) sb.Append($" t[{tags}]");
        
        var keywords = string.Join(", ", card.Keywords.Select(keyword => $"`{keyword}`"));
        if (keywords.Length > 0) sb.Append($" k[{keywords}]");
       
        var enchantments = (card.Enchantment != null) ? $"`{card.Enchantment.Title.GetFormattedText()}`" : "";
        if (enchantments.Length > 0) sb.Append($" e[{enchantments}]");
        
        var afflictions = (card.Affliction != null) ? $"`{card.Affliction.Title.GetFormattedText()}`" : "";
        if (afflictions.Length > 0) sb.Append($" a[{afflictions}]");

        var replayCount = card.GetEnchantedReplayCount();
        var replayEntry = (replayCount > 0) ? $"`Replay {replayCount}`" : "";
        if (replayEntry.Length > 0) sb.Append($" r[{replayEntry}]");
        
        var energyCost = (card.EnergyCost.CostsX) ? "X" : card.EnergyCost.GetAmountToSpend().ToString();
        var starCost = (card.HasStarCostX) ? "X" : card.CurrentStarCost.ToString();

        var isUnplayable = card.Keywords.Any(keyword => keyword == CardKeyword.Unplayable);

        return isUnplayable
            ? $"{sb} **unplayable**"
            : card.CurrentStarCost > 0 || card.HasStarCostX
                ? $"{sb} **costing** `{energyCost}` energy **and** `{starCost}` stars"
                : $"{sb} **costing** `{energyCost}` energy";
    }
}