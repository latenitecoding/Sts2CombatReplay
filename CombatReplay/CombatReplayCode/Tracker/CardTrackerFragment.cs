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

        if (card.CurrentStarCost > 0 || card.HasStarCostX)
        {
            WriteIt($"> I **drew** {FormatCard(card)} [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy **and** `{starCost}` stars <\\");
        }
        else
        {
            WriteIt($"> I **drew** {FormatCard(card)} [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy <\\");
        }

        _db.TotalCardsDrawn += 1;
        return Task.CompletedTask;
    }
    
    public void OnAddGeneratedCard(Player owner, CardModel card, bool addedByPlayer = true)
    {
        if (!LocalContext.IsMe(owner) || !_db.IsInCombat()) return;
        if (!addedByPlayer)
        {
            WriteIt($"> {FormatPlayer(owner)} **given** `{card.Title}` <\\");
            return;
        }
        WriteIt($"> {FormatPlayer(owner)} **created** `{card.Title}` <\\");
        _db.TotalCardsCreated += 1;
    }
    
    public void OnExecuteCard(PlayCardAction action)
    {
        var card = action.NetCombatCard.ToCardModel();
        WriteIt(action.Target != null
            ? $"> _{FormatPlayer(action.Player)} **played** {FormatCard(card)} **targeting** {FormatCreature(action.Target)}_ <\\"
            : $"> _{FormatPlayer(action.Player)} **played** {FormatCard(card)}_ <\\");
        if (!LocalContext.IsMe(card.Owner)) return;
        _db.OnExecuteCard(card.Title);
    }
    
    private static string FormatCard(CardModel card)
    {
        return $"`{card.Title}`";
    }
}