using CombatReplay.CombatReplayCode.ReplayDb;
using CombatReplay.CombatReplayCode.Utils;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Multiplayer.Game;
using MegaCrit.Sts2.Core.Runs;
using MegaCrit.Sts2.Core.Saves;

namespace CombatReplay.CombatReplayCode.Tracker;

public partial class CombatReplayTracker : AbstractModel
{
    // Required by AbstractModel; used for hooking into ModHelper
    public override bool ShouldReceiveCombatHooks => true;

    private string? _runSeed;
    private ReplayLogger? _logger;
    private CombatReplayDb _db = new();

    public void OnRunStart()
    {
        MainFile.Logger.Info($"CombatReplay tracking started");

        var saveManager = SaveManager.Instance;
        
        MainFile.Logger.Info($"SaveManager Profile ID {saveManager.CurrentProfileId}");
        MainFile.Logger.Info($"SaveManager Has Save: {saveManager.HasRunSave}");

        var profileId = saveManager.CurrentProfileId;
        var hasSave = saveManager.HasRunSave;
        var isMultiplayer = RunManager.Instance.NetService.Type.IsMultiplayer();

        MainFile.Logger.Info(isMultiplayer
            ? "RunManager is starting a Multiplayer run"
            : "RunManager is starting a Singleplayer run");

        MainFile.Logger.Info("Starting ReplayLogger...");
        
        _logger = new ReplayLogger(profileId, isMultiplayer, "sts2_combat_tracker_current.replay", hasSave);
        _logger.OnRunStart();

        MainFile.Logger.Info("Initializing CombatReplayDb...");

        _db = hasSave
            ? CombatReplayDb.LoadFromFileOrElse(profileId, isMultiplayer, "sts2_combat_stats_current_mp.json", _db)
            : _db.Init(profileId, isMultiplayer, "sts2_combat_stats_current_mp.json");
        _db.RunSeed = _runSeed;

        if (hasSave) return;
        
        WriteIt($"# Run **started** as Player NetId `{LocalContext.NetId}` on Profile{profileId} #");
        if (_runSeed != null) WriteIt($"=== Run **seeded** `{_runSeed}` ===\\");
        
        AfterActEntered();
    }

    public void OnRunEnd(long startTime)
    {
        WriteIt($"=== Run **ended** ===\\");
        TaskHelper.RunSafely(SaveRun(startTime));
    }

    private Task SaveRun(long startTime)
    {
        MainFile.Logger.Info("CombatReplay tracking stopped");
        MainFile.Logger.Info("Saving Run...");
        
        _logger?.OnRunEnd(startTime);
        _db.OnRunEnd(startTime);
        
        return Task.CompletedTask;
    }

    private void WriteIt(string msg)
    {
        _logger?.WriteIt(msg);
    }
}