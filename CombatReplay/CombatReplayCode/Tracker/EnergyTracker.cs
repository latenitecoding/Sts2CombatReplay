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
        WriteIt($"> {FormatPlayer(player)} **reset** `{currentEnergy}/{player.MaxEnergy}` energy <\\");
        return Task.CompletedTask;
    }
    
    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> `{card.Title}` **cost** `{amount}` energy <\\");
        _db.TotalEnergySpent += amount;
        return Task.CompletedTask;
    }

    public void OnGainEnergy(Decimal amount)
    {
        WriteIt($"> I **gained** `{(int) amount}` energy <\\");
        _db.TotalEnergyGained += (int) amount;
    }
   
    private static string FormatPlayer(Player player, bool useTitle = false)
    {
        var playerName = (LocalContext.IsMe(player) && !useTitle) ? "I" : $"Player: `{player.Character.Title.GetFormattedText()}`";
        return (player.Creature.CombatId != null)
            ? $"{playerName} (`{player.Creature.CombatId}`)"
            : $"{playerName} (`{player.NetId}`)";
    }
}