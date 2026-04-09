using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterStarsGained(int amount, Player gainer)
    {
        if (!LocalContext.IsMe(gainer)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(gainer)} **gained** `{amount}` stars <\\");
        _db.TotalStarsGained += amount;
        return Task.CompletedTask;
    }
    
    public override Task AfterStarsSpent(int amount, Player spender)
    {
        if (!LocalContext.IsMe(spender)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(spender)} **spent** `{amount}` stars <\\");
        _db.TotalStarsSpent += amount; 
        return Task.CompletedTask;
    }
    
    public override Task AfterForge(Decimal amount, Player forger, AbstractModel? source)
    {
        if (!LocalContext.IsMe(forger)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(forger)} **forged** `{(int) amount}` <\\");
        _db.TotalForged += (int) amount;
        return Task.CompletedTask;
    }
    
    public void RecordCardCreated(Player owner, CardModel card, bool addedByPlayer = true)
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
}