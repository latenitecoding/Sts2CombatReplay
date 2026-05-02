using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.Models;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public override Task AfterEnergyReset(Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        var currentEnergy = player.PlayerCombatState?.Energy ?? player.MaxEnergy;
        WriteIt($"> {FormatPlayer(player)} **reset** `{currentEnergy}/{player.MaxEnergy}` energy");
        if (currentEnergy > player.MaxEnergy) _db.BonusEnergyGained += currentEnergy - player.MaxEnergy;
        return Task.CompletedTask;
    }
    
    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> `{card.Title}` **cost** `{amount}` energy");
        _db.TotalEnergySpent += amount;
        return Task.CompletedTask;
    }

    public void OnGainEnergy(PlayerCombatState combatState, Decimal amount)
    {
        var player = combatState.AllCards.First().Owner;
        WriteIt($"> {FormatPlayer(player)} **gained** `{(int) amount}` energy");
        if (!LocalContext.IsMe(player)) return;
        _db.BonusEnergyGained += (int) amount;
    }
}