using CombatReplay.CombatReplayCode.Utils;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnActEntered()
    {
        // there is an AfterActEntered, but it doesn't appear to be called by the game
        _db.NextAct();
        // These event orders can occur:
        // 1) First run + Neow's Room. OnActEntered is called but not OnRoomEntered
        // 2) Mid run. OnRoomEntered is called then OnActEntered
        BufferIt(
            $"## Act {_db.CurrentAct} **started**",
            ReplayLogger.MsgType.ActStarted,
            ReplayLogger.MsgType.RoomEntered);
        
        // if the OnRoomEntered isn't called, then we increment CurrentRoom
        if (_db.CurrentRoom == 0) _db.NextRoom();
        
        BufferIt($"### Room {_db.CurrentRoom} **entered**", ReplayLogger.MsgType.ActStarted);

        _runSeed ??= _db.RunSeed;
        _db.RunSeed ??= _runSeed;
    }

    public void OnRoomEntered()
    {
        _db.NextRoom();
        BufferIt($"### Room {_db.CurrentRoom} **entered**", ReplayLogger.MsgType.RoomEntered);
        
        // if OnRoomEntered is called but not OnActEntered at the start of Act I, the previous RoomEntered
        // will be replaced in the OnActEntered
        if (_db.CurrentAct == 0) OnActEntered();
    }

    public void OnRoomExited()
    {
        // sometimes the OnActEntered isn't called in Neow's room
        if (_db.CurrentAct == 0)
        {
            OnActEntered();
            return;
        }
        if (CheckIt(ReplayLogger.MsgType.ActStarted))
        {
            // for Act II and Act III, OnRoomExited is sometimes called twice
            Flush();
            return;
        }
        
        WriteIt($"==Room {_db.CurrentRoom} **exited**==");
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave();

        // this is called here to ensure that the room is logged before the setup of the room
        OnRoomEntered();
    }
}