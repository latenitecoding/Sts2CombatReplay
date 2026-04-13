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
    // Required by AbstractModel; used for hooking into ModHelper
    public override bool ShouldReceiveCombatHooks => true;

    private string _multiplayerRunId = "mp";
    private string? _runSeed;

    private ReplayLogger? _logger;
    private CombatReplayDb _db = new();

    public void OnInitializeRun(INetGameService netService, long startTime)
    {
        if (!netService.Type.IsMultiplayer()) return;
        _multiplayerRunId = $"{startTime}_mp";
    }
    
    public void OnRunStart()
    {
        MainFile.Logger.Info($"CombatReplay tracking started");

        var saveManager = SaveManager.Instance;
        
        MainFile.Logger.Info($"SaveManager Profile ID {saveManager.CurrentProfileId}");
        MainFile.Logger.Info($"SaveManager has Singleplayer Save: {saveManager.HasRunSave}");
        MainFile.Logger.Info($"SaveManager has Multiplayer Save: {saveManager.HasMultiplayerRunSave}");

        var isMultiplayer = RunManager.Instance.NetService.Type.IsMultiplayer();

        MainFile.Logger.Info(isMultiplayer
            ? "RunManager is starting a Multiplayer run"
            : "RunManager is starting a Singleplayer run");
        
        _multiplayerRunId = _runSeed != null ? $"{_runSeed}_{_multiplayerRunId}" : _multiplayerRunId;
        
        MainFile.Logger.Info("Starting ReplayLogger...");

        _logger = new ReplayLogger(
            saveManager.CurrentProfileId,
            isMultiplayer,
            "sts2_combat_tracker_current.replay",
            isMultiplayer ? saveManager.HasMultiplayerRunSave : saveManager.HasRunSave,
            multiRunId: _multiplayerRunId);
        _logger.OnRunStart();

        MainFile.Logger.Info("Initializing CombatReplayDb...");

        // if the player plays multiple runs one after another, then we need to recreate the db
        // if the db is on anything other than Act 0, it has already been used for a run
        if (_db.CurrentAct > 0) _db = new CombatReplayDb();
        _db = (isMultiplayer && saveManager.HasMultiplayerRunSave) || (!isMultiplayer && saveManager.HasRunSave)
            ? CombatReplayDb.LoadFromFileOrElse(
                saveManager.CurrentProfileId,
                isMultiplayer,
                "sts2_combat_stats_current.json",
                _db,
                multiRunId: _multiplayerRunId)
            : _db.Init(
                saveManager.CurrentProfileId,
                isMultiplayer,
                "sts2_combat_stats_current.json",
                multiRunId: _multiplayerRunId);
        _db.RunSeed = _runSeed;

        if ((isMultiplayer && saveManager.HasMultiplayerRunSave) || (!isMultiplayer && saveManager.HasRunSave)) return;
        
        WriteIt($"# Run **started** as Player NetId `{LocalContext.NetId}` on Profile{saveManager.CurrentProfileId} #");

        var ascensionLevel = AscensionLevel.None;
        foreach (var ascension in Enum.GetValues<AscensionLevel>())
        {
            if (RunManager.Instance.AscensionManager.HasLevel(ascension)) ascensionLevel = ascension;
        }
        WriteIt($"=== Ascension Level: `{ascensionLevel}` ===\\");
        _db.AscensionLevel = ascensionLevel.ToString();
        
        if (_runSeed != null) WriteIt($"=== Run **seeded** `{_runSeed}` ===\\");
    }

    public void OnRunEnd(long startTime)
    {
        WriteIt($"=== Run **ended** ===\\");
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
    
    private void BufferBefore(string msg, ReplayLogger.MsgType msgType, ReplayLogger.MsgType preceded, bool autoFlush = true)
    {
        _logger?.BufferBefore(msg, msgType, preceded, autoFlush);
    }

    private void BufferIt(string msg, ReplayLogger.MsgType msgType, ReplayLogger.MsgType overwrite = ReplayLogger.MsgType.None, bool autoFlush = true)
    {
        _logger?.BufferIt(msg, msgType, overwrite, autoFlush);
    }

    private void WriteBefore(string msg, ReplayLogger.MsgType preceded)
    {
        _logger?.WriteBefore(msg, preceded);
    }

    private void WriteIt(string msg, ReplayLogger.MsgType overwrite =  ReplayLogger.MsgType.None)
    {
        _logger?.WriteIt(msg, overwrite);
    }
}