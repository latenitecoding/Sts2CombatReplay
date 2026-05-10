using CombatReplay.CombatReplayCode.Utils;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnActEntered()
    {
        // there is an AfterActEntered, but it doesn't appear to be called by the game
        
        _db.NextAct();
        // need to overwrite RoomEntered as that event can come before OnActEntered
        var (_, found) = WriteIt($"## Act {_db.CurrentAct} **started**", ReplayLogger.MsgType.RoomEntered);
        if (found) _db.CurrentRoom--;
        
        _runSeed ??= _db.RunSeed;
        _db.RunSeed ??= _runSeed;
        
        // sometimes OnActEntered will be called, but not OnRoomEntered
        OnRoomEntered();
        // replace the new RoomEntered event with an ActStarted event to block the double counted OnRoomExit
        BufferIt(
            $"### Room {_db.CurrentRoom} **entered**",
            ReplayLogger.MsgType.ActStarted,
            overwriting: ReplayLogger.MsgType.RoomEntered,
            expecting: ReplayLogger.MsgType.RoomExited);
    }

    public void OnRoomEntered()
    {
        // there is a BeforeRoomEntered and an AfterActEntered, but they don't appear to be called by the game
        
        // in case OnRoomEntered is called but not OnActEntered
        if (_db.CurrentAct == 0) OnActEntered();
        // in case OnRoomEntered is ever called twice in a row
        else if (CheckIt(ReplayLogger.MsgType.RoomEntered)) return;
        
        _db.NextRoom();
        BufferIt(
            $"### Room {_db.CurrentRoom} **entered**",
            ReplayLogger.MsgType.RoomEntered,
            overwriting: ReplayLogger.MsgType.None,
            expecting: ReplayLogger.MsgType.ActStarted |ReplayLogger.MsgType.RunEnded);
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave();
    }

    public void OnRoomExited()
    {
        // it's possible to leave Neow's room without having triggered an OnActEntered
        if (_db.CurrentAct == 0) OnActEntered();
        // it's possible for OnRoomExited to be called twice after the start of Act II or Act III
        else if (CheckIt(ReplayLogger.MsgType.ActStarted))
        {
            Flush();
            return;
        }
        
        WriteIt($"==Room {_db.CurrentRoom} **exited**==");

        // this is called here to ensure that the room is logged before the setup of the room
        OnRoomEntered();
    }
}