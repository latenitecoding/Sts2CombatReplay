using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterForge(Decimal amount, Player forger, AbstractModel? source)
    {
        if (!LocalContext.IsMe(forger) || (int)amount <= 0) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(forger)} **forged** `{(int) amount}`");
        _db.TotalForged += (int) amount;
        return Task.CompletedTask;
    }
    
    public override Task AfterStarsGained(int amount, Player gainer)
    {
        if (!LocalContext.IsMe(gainer) || amount <= 0) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(gainer)} **gained** `{amount}` stars");
        _db.TotalStarsGained += amount;
        return Task.CompletedTask;
    }
    
    public override Task AfterStarsSpent(int amount, Player spender)
    {
        if (!LocalContext.IsMe(spender) || amount <= 0) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(spender)} **spent** `{amount}` stars");
        _db.TotalStarsSpent += amount; 
        return Task.CompletedTask;
    }
}