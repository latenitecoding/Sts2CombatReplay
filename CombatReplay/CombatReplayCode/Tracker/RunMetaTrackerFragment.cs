using MegaCrit.Sts2.Core.Nodes.Vfx;
using MegaCrit.Sts2.Core.Runs;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnActEntered()
    {
        // there is an AfterActEntered, but it doesn't appear to be called by the game
        _db.NextAct();
        if (_db.CurrentAct > 1) OnRoomExited();
        WriteIt($"### Act {_db.CurrentAct} **started** ###");

        _runSeed ??= _db.RunSeed;
        _db.RunSeed ??= _runSeed;
        
        // this is called here to ensure that the first room is called after the act is started
        OnRoomEntered();
    }

    public void OnRoomEntered()
    {
        _db.NextRoom();
        WriteIt($"##### Room {_db.CurrentRoom} **entered** #####");
    }

    public void OnRoomExited()
    {
        // sometimes the OnActEntered isn't called in Neow's room
        if (_db.CurrentAct == 0) OnActEntered();
        WriteIt($"=== Room {_db.CurrentRoom} **exited** ===\\");
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave();

        // this is called here to ensure that the room is logged before the setup of the room
        OnRoomEntered();
    }
}