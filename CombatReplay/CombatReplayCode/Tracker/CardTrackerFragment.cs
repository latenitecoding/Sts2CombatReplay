using MegaCrit.Sts2.Core.Context;
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

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> I **drew** {FormatCard(card)} <\\");
        _db.OnCardDrawn(card.Title);
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer = true)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        if (!addedByPlayer)
        {
            WriteIt($"> {FormatPlayer(owner)} **was** **given** {FormatCard(card)} <\\");
            return;
        }
        WriteIt($"> {FormatPlayer(owner)} **created** {FormatCard(card)} <\\");
        _db.TotalCardsCreated += 1;
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

    public void OnTransformCard(Player owner, CardModel original, CardModel replacement)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        WriteIt($"> {FormatPlayer(owner)} **transformed** `{original.Title}` <\\");
        WriteIt($"> {FormatPlayer(owner)} **gained** {FormatCard(replacement)} <\\");
        _db.TotalCardsCreated += 1;
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