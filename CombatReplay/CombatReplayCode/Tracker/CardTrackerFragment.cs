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

    private readonly HashSet<string> _playerCreatedCards = [];

    public override Task AfterCardEnteredCombat(CardModel card)
    {
        WriteIt($"> {FormatPlayer(card.Owner)} **gained** {FormatCard(card)} <\\");
        _db.OnCardCreated(card.Title, _playerCreatedCards.Contains(card.Title), card.Pile is { Type: PileType.Hand });
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> I **drew** {FormatCard(card)} <\\");
        _db.OnCardDrawn(card.Title);
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        if (addedByPlayer) _playerCreatedCards.Add(card.Title);
        WriteIt(addedByPlayer
            ? $"> {FormatPlayer(owner)} **created** `{card.Title}` <\\"
            : $"> {FormatPlayer(owner)} **was** **given** `{card.Title}` <\\");
    }

    public void OnCardAdded(Player owner, CardModel card, PileType pileType)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        WriteIt($"> {FormatPlayer(card.Owner)} **added** `{card.Title}` **to** `{pileType.ToString()}` <\\");
        _db.OnCardAdded(card.Title, pileType == PileType.Hand);
    }
    
    public void OnExecuteCard(PlayCardAction action)
    {
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