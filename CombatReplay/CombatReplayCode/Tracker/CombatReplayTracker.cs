using CombatReplay.CombatReplayCode.ReplayDb;
using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Ascension;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker : AbstractModel
{
    public static string DbFile = "sts2_combat_stats_current.json";
    public static string TrackerFile = "sts2_combat_tracker_current.replay";
    
    // Required by AbstractModel; used for hooking into ModHelper
    public override bool ShouldReceiveCombatHooks => true;

    private string _multiplayerRunId = "mp";
    private long? _multiplayerStartTime;
    private string? _runSeed;

    private ReplayLogger? _logger;
    private CombatReplayDb _db = new();

    public void OnInitializeRun(INetGameService netService, long startTime)
    {
        if (!netService.Type.IsMultiplayer()) return;
        _multiplayerRunId = $"{startTime}_mp";
        _multiplayerStartTime = startTime;
    }
    
    public void OnRunStart()
    {
        MainFile.Logger.Info($"CombatReplay tracking started");

        var saveManager = SaveManager.Instance;
        
        MainFile.Logger.Info($"SaveManager Profile ID {saveManager.CurrentProfileId}");
        MainFile.Logger.Info($"SaveManager has Singleplayer Save: {saveManager.HasRunSave}");

        var isMultiplayer = RunManager.Instance.NetService.Type.IsMultiplayer();

        MainFile.Logger.Info(isMultiplayer
            ? "RunManager is starting a Multiplayer run"
            : "RunManager is starting a Singleplayer run");

        var multiplayerInProgress =
            FileUtils.CheckSavePath(saveManager.CurrentProfileId, isMultiplayer, TrackerFile, _multiplayerRunId);
        if (_multiplayerStartTime.HasValue && !multiplayerInProgress)
        {
            // sometimes when reloading a multiplayer run, the start time will be one more than it should be
            var wrongMultiplayerRunId = $"{_multiplayerStartTime.Value - 1}_mp";
            multiplayerInProgress = FileUtils.CheckSavePath(
                saveManager.CurrentProfileId,
                isMultiplayer,
                TrackerFile,
                wrongMultiplayerRunId);
            if (multiplayerInProgress)
            {
                var wrongSaveFile = FileUtils.GetSavePath(
                    saveManager.CurrentProfileId,
                    isMultiplayer,
                    TrackerFile,
                    wrongMultiplayerRunId);
                var rightSaveFile = FileUtils.GetSavePath(
                    saveManager.CurrentProfileId,
                    isMultiplayer,
                    TrackerFile,
                    _multiplayerRunId);
                File.Move(wrongSaveFile, rightSaveFile);
            }
        }
        MainFile.Logger.Info($"Multiplayer game in progress {multiplayerInProgress}");
        
        MainFile.Logger.Info("Starting ReplayLogger...");

        _logger = new ReplayLogger(
            saveManager.CurrentProfileId,
            isMultiplayer,
            TrackerFile,
            isMultiplayer ? multiplayerInProgress : saveManager.HasRunSave,
            multiRunId: _multiplayerRunId);
        _logger.OnRunStart();

        MainFile.Logger.Info("Initializing CombatReplayDb...");

        // if the player plays multiple runs one after another, then we need to recreate the db
        // if the db is on anything other than Act 0, it has already been used for a run
        if (_db.CurrentAct > 0) _db = new CombatReplayDb();
        _db = (isMultiplayer && multiplayerInProgress) || (!isMultiplayer && saveManager.HasRunSave)
            ? CombatReplayDb.LoadFromFileOrElse(
                saveManager.CurrentProfileId,
                isMultiplayer,
                DbFile,
                _db,
                multiRunId: _multiplayerRunId)
            : _db.Init(
                saveManager.CurrentProfileId,
                isMultiplayer,
                DbFile,
                multiRunId: _multiplayerRunId);
        _db.RunSeed = _runSeed;

        if ((isMultiplayer && multiplayerInProgress) || (!isMultiplayer && saveManager.HasRunSave)) return;
        
        WriteIt($"# Run **started** as Player NetId `{LocalContext.NetId}` on Profile{saveManager.CurrentProfileId}");

        var ascensionLevel = AscensionLevel.None;
        foreach (var ascension in Enum.GetValues<AscensionLevel>())
        {
            if (RunManager.Instance.AscensionManager.HasLevel(ascension)) ascensionLevel = ascension;
        }
        WriteIt($"- Ascension Level: `{ascensionLevel}`");
        _db.AscensionLevel = ascensionLevel.ToString();
        
        if (_runSeed != null) WriteIt($"- Run Seed: `{_runSeed}`");
        
        WriteIt($"- Mod Version: `{MainFile.Version}`");
    }

    public void OnRunEnd(long startTime)
    {
        var (ok, found) = BufferIt($"==Run **ended**==", ReplayLogger.MsgType.RoomEntered);
        if (found) _db.CurrentRoom--;
        
        MainFile.Logger.Info("CombatReplay tracking stopped");
        MainFile.Logger.Info("Saving Run...");
        
        _logger?.OnRunEnd(startTime);
        _db.OnRunEnd(startTime);
    }
    
    public void UpdateSeed(string seed)
    {
        if (_runSeed != null) return;
        _runSeed = seed;
        _db.RunSeed = seed;
    }
    
    private (bool ok, bool found) BufferBefore(string msg, ReplayLogger.MsgType msgType, ReplayLogger.MsgType preceded, bool autoFlush = true)
    {
        return _logger?.BufferBefore(msg, msgType, preceded, autoFlush) ?? (false, false);
    }

    private (bool ok, bool found) BufferIt(string msg, ReplayLogger.MsgType msgType, ReplayLogger.MsgType overwrite = ReplayLogger.MsgType.None, bool autoFlush = true)
    {
        return _logger?.BufferIt(msg, msgType, overwrite, autoFlush) ?? (false, false);
    }

    private bool CheckIt(ReplayLogger.MsgType msgType)
    {
        return _logger?.CheckIt(msgType) ?? false;
    }

    private void Flush()
    {
        _logger?.Flush();
    }

    private (bool ok, bool found) WriteBefore(string msg, ReplayLogger.MsgType preceded)
    {
        return _logger?.WriteBefore(msg, preceded) ?? (false, false);
    }

    private (bool ok, bool found) WriteIt(string msg, ReplayLogger.MsgType overwrite =  ReplayLogger.MsgType.None)
    {
        return _logger?.WriteIt(msg, overwrite) ?? (false, false);
    }
}