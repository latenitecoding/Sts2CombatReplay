using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;

namespace CombatReplay.CombatReplayCode.Trackers;

public partial class CombatReplayTracker
{
    public void RecordSeed(string seed)
    {
        if (_runSeed != null) return;
        _runSeed = seed;
    }

    public override Task AfterActEntered()
    {
        _db.NextAct();
        WriteIt($"### Act {_db.CurrentAct} **started** ###");
        
        // each act starts with an 'Ancient' room which is handled implicitly by the game
        _db.NextRoom();
        WriteIt($"##### Room: {_db.CurrentRoom} (`Ancient`) **entered** #####");
        
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        WriteIt($"##### Room: {_db.CurrentRoom} (`{room.RoomType.ToString()}`) **entered** #####");
        return Task.CompletedTask;
    }
    
    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (!LocalContext.IsMe(potion.Owner)) return Task.CompletedTask;
        WriteIt($"> _I **discarded** `{potion.Title}`_ <\\");
        _db.TotalPotionsDiscarded += 1;
        return Task.CompletedTask;
    }
    
    public void OnCreatureHeal(Creature creature, Decimal amount)
    {
        if (_db.CurrentRoom <= 1) return;
        var trueAmount = Math.Min((int) amount, creature.MaxHp - creature.CurrentHp);
        WriteIt($"> {FormatCreature(creature)} **healed** `{trueAmount}` HP <\\");
        if (!LocalContext.IsMe(creature)) return;
        _db.TotalHpHealed += trueAmount;
    }
    
    // current firing when a room is entered (except the first room)
    public void OnRoomExited()
    {
        WriteIt($"=== Room: {_db.CurrentRoom} **exited** ===\\");
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave();
        
        _db.NextRoom();
        WriteIt($"##### Room: {_db.CurrentRoom} **entered** #####");
    }
}