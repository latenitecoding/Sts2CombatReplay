using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Models.Powers;
using MegaCrit.Sts2.Core.Rooms;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{

    public override Task AfterCombatEnd(CombatRoom room)
    {
        WriteIt($"==Combat {_db.FinalCombat} **ended**==");
        _db.OnEndCombat();
    
        MainFile.Logger.Info($"CombatReplay logging stats for combat {_db.FinalCombat}");
        _db.InProgressSave();

        _logger?.OnCombatEnd();
        
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        WriteIt($"==Combat {_db.FinalCombat} **was** `victory`==");
        return Task.CompletedTask;
    }
    
    public override Task AfterFlush(
        PlayerChoiceContext choiceContext,
        Player player,
        IReadOnlyCollection<CardModel> flushedCards,
        IReadOnlyCollection<CardModel> retainedCards)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        
        foreach (var card in retainedCards)
        {
            _db.TotalCardsRetained++;
            WriteIt($"> {FormatPlayer(card.Owner)} **retained** {FormatCard(card)} **in** `Hand`");
        }
        
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        
        WriteIt("==Player Phase **started**==");
        
        foreach (var creature in _db.GetCombatCreatureList())
        {
            if (creature.IsDead) continue;
            
            var powers = string.Join(", ", creature.Powers.Select(FormatPower));
            WriteIt($"> {FormatCreature(creature)} **active** [{powers}] powers");
            
            if (creature is { IsPlayer: true } and not { Player: null } and not { Player.PlayerCombatState: null } &&
                creature.Player.PlayerCombatState.OrbQueue.Orbs.Count > 0)
            {
                foreach (var orb in creature.Player.PlayerCombatState.OrbQueue.Orbs)
                {
                    WriteIt($"> {FormatCreature(creature)} **has** {FormatOrb(orb)}");
                }
            }
        }

        var pcs = player.PlayerCombatState;
        if (pcs == null) return Task.CompletedTask;
        
        var handSize = pcs.Hand.Cards.Count;
        var deckSize = pcs.DrawPile.Cards.Count;
        var discardSize = pcs.DiscardPile.Cards.Count;
        var exhaustSize = pcs.ExhaustPile.Cards.Count;
        WriteIt($"> {FormatPlayer(player)} **have** `{handSize}|{deckSize}|{discardSize}|{exhaustSize}` Hand|Deck|Discard|Exhaust cards");
        
        return Task.CompletedTask;       
    }

    public override Task AfterTakingExtraTurn(Player player)
    {
        WriteIt($"=={FormatPlayer(player)} **taking** Extra Turn==");
        return Task.CompletedTask;
    }
    
    public override Task BeforeCombatStart()
    {
        _db.OnStartCombat();
        WriteIt($"==Combat {_db.FinalCombat} **started**==");
        
        return Task.CompletedTask;
    }

    public override Task BeforeSideTurnStart(PlayerChoiceContext choiceContext, CombatSide side, IReadOnlyList<Creature> participants, ICombatState combatState)
    {
        switch (side)
        {
            case CombatSide.Player:
                _db.OnNextTurn();
                WriteIt($"==Turn {_db.CurrentTurn} **started**==");
                return Task.CompletedTask;
            case CombatSide.Enemy:
                WriteIt("==Enemy Phase **started**==");
                return Task.CompletedTask;
            default:
                return Task.CompletedTask;
        }
    }
    
    public override Task BeforeSideTurnEnd(PlayerChoiceContext choiceContext, CombatSide side, IEnumerable<Creature> participants)
    {
        switch (side)
        {
            case CombatSide.Player:
                WriteIt("==Player Phase **ended**==");

                foreach (var creature in participants)
                {
                    if (!creature.IsEnemy) continue;
                    
                    var poisonPower = creature.Powers.FirstOrDefault(power => power is PoisonPower, null);
                    if (poisonPower is null) continue;

                    _db.OnPoisonDamageDealt(creature, poisonPower.Amount);
                }
                
                break;
            case CombatSide.Enemy:
                WriteIt("==Enemy Phase **ended**==");
                WriteIt($"==Turn {_db.CurrentTurn} **ended**==");
                break;
            case CombatSide.None:
            default:
                break;
        }
        return Task.CompletedTask;
    }
}