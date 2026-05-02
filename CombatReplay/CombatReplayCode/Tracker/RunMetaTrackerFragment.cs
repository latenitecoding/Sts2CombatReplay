using CombatReplay.CombatReplayCode.Utils;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    public void OnActEntered()
    {
        // there is an AfterActEntered, but it doesn't appear to be called by the game
        _db.NextAct();
        BufferIt(
            $"## Act {_db.CurrentAct} **started**",
            ReplayLogger.MsgType.ActStarted,
            ReplayLogger.MsgType.RoomEntered);
        if (_db.CurrentRoom > 0)
        {
            BufferIt($"### Room {_db.CurrentRoom} **entered**", ReplayLogger.MsgType.ActStarted);
        }

        _runSeed ??= _db.RunSeed;
        _db.RunSeed ??= _runSeed;
    }

    public void OnRoomEntered()
    {
        if (_db.CurrentAct == 0) OnActEntered();
        
        _db.NextRoom();
        BufferIt($"### Room {_db.CurrentRoom} **entered**", ReplayLogger.MsgType.RoomEntered);
    }

    public void OnRoomExited()
    {
        // sometimes the OnActEntered isn't called in Neow's room
        if (_db.CurrentAct == 0) OnRoomEntered();
        if (CheckIt(ReplayLogger.MsgType.ActStarted))
        {
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