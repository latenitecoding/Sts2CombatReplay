namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker
{
    private string? _runSeed;

    public void OnActEntered()
    {
        // there is an AfterActEntered, but it doesn't appear to be called by the game
        _db.NextAct();
        WriteIt($"### Act {_db.CurrentAct} **started** ###");
    }

    public void OnRoomEntered()
    {
        _db.NextRoom();
        WriteIt($"##### Room: {_db.CurrentRoom} **entered** #####");
    }

    public void OnRoomExited()
    {
        WriteIt($"=== Room: {_db.CurrentRoom} **exited** ===\\");
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave();
    }
    
    public void UpdateSeed(string seed)
    {
        if (_runSeed != null) return;
        _runSeed = seed;
        _db.RunSeed = seed;
    }
}