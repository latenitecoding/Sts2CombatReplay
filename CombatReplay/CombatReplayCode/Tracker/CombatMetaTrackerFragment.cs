using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> I **discarded** `{card.Title}` <\\");
        _db.OnCardDiscarded(card.Title);
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        WriteIt($"=== Combat: {_db.CurrentCombat} **ended** ===\\");
        _db.OnEndCombat();
    
        MainFile.Logger.Info($"CombatReplay logging stats for combat {_db.CurrentCombat}");
        _db.InProgressSave();

        _logger?.OnCombatEnd();
        
        return Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _db.TotalCardsExhausted++;
        WriteIt($"> I **exhausted** `{card.Title}` <\\");
        return Task.CompletedTask;
    }
    
    public override Task AfterCardRetained(CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _db.TotalCardsRetained++;
        WriteIt($"> I **retained** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        WriteIt($"=== Combat: {_db.CurrentCombat} **was** `victory` ===\\");
        return Task.CompletedTask;
    }
    
    public override Task AfterHandEmptied(PlayerChoiceContext ctx, Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        _db.TotalEmptyHands++;
        WriteIt($"> {FormatPlayer(player)} **emptied** `Hand` <\\");
        return Task.CompletedTask;
    }
   
    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        
        _db.OnNextTurn();
        WriteIt($"=== Turn: {_db.CurrentTurn} **started** ===");

        foreach (var creature in _db.GetCombatCreatureList())
        {
            if (creature.IsDead)
            {
                WriteIt($"> {FormatCreature(creature)} **defeated** <\\");
                continue;
            }
            
            var powers = string.Join(", ", creature.Powers.Select(power => $"`{power.Title.GetFormattedText()} {power.Amount}`"));
            WriteIt($"> {FormatCreature(creature)} **active** [{powers}] powers <\\");
            
            if (creature is { IsPlayer: true } and not { Player : null } and not { Player.PlayerCombatState: null } &&
                creature.Player.PlayerCombatState.OrbQueue.Orbs.Count > 0)
            {
                foreach (var orb in creature.Player.PlayerCombatState.OrbQueue.Orbs)
                {
                    WriteIt($"> {FormatCreature(creature)} **has** {FormatOrb(orb)} <\\");
                }
            }
        }

        var pcs = player.PlayerCombatState;
        if (pcs == null) return Task.CompletedTask;
        
        var handSize = pcs.Hand.Cards.Count;
        var deckSize = pcs.DrawPile.Cards.Count;
        var discardSize = pcs.DiscardPile.Cards.Count;
        var exhaustSize = pcs.ExhaustPile.Cards.Count;
        WriteIt($"> {FormatPlayer(player)} **have** `{handSize}|{deckSize}|{discardSize}|{exhaustSize}` hand|deck|discard|exhaust cards <\\");
        
        return Task.CompletedTask;       
    }

    public override Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)
    {
        if (!LocalContext.IsMe(shuffler)) return Task.CompletedTask;
        _db.TotalDeckShuffles++;
        WriteIt($"> {FormatPlayer(shuffler)} **emptied** `Deck` <\\");
        WriteIt($"> {FormatPlayer(shuffler)} **shuffled** `Discard` <\\");
        return Task.CompletedTask;
    }
    
    public override Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        switch (side)
        {
            case CombatSide.Player:
                WriteIt("=== Player phase **ended** ===\\");
                break;
            case CombatSide.Enemy:
                WriteIt($"=== Turn: {_db.CurrentTurn} **ended** ===\\");
                break;
            case CombatSide.None:
            default:
                break;
        }
        return Task.CompletedTask;
    }
  
    public override Task BeforeCombatStart()
    {
        _db.OnStartCombat();
        WriteIt($"=== Combat: {_db.CurrentCombat} **started** ===");
        
        return Task.CompletedTask;
    }
}