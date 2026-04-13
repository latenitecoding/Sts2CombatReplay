using System.Text.Json;
using CombatReplay.CombatReplayCode.Utils;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    private static readonly JsonSerializerOptions JsonOptions = new() { 
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CombatReplayDb LoadFromFileOrElse(int profileId, bool isMultiplayer, string saveFile, CombatReplayDb db, string multiRunId = "mp")
    {
        var savePath = FileUtils.GetSavePath(profileId, isMultiplayer, saveFile, multiRunId);
        return File.Exists(savePath)
            ? JsonSerializer.Deserialize<CombatReplayDb>(File.ReadAllText(savePath), JsonOptions)?
                .Init(profileId, saveFile, savePath) ?? db.Init(profileId, isMultiplayer, saveFile)
            : db.Init(profileId, isMultiplayer, saveFile);
    }

    private int? _profileId;
    private string? _saveFile;
    private string? _savePath;

    public CombatReplayDb Init(int profileId, bool isMultiplayer, string saveFile, string multiRunId = "mp")
    {
        IsMultiplayer = isMultiplayer;
        return Init(profileId, saveFile, FileUtils.GetSavePath(profileId, isMultiplayer, saveFile, multiRunId));
    }

    private CombatReplayDb Init(int profileId, string saveFile, string savePath)
    {
        _profileId = profileId;
        _saveFile = saveFile;
        _savePath = savePath;
        return this;
    }

    public void InProgressSave()
    {
        if (_savePath == null) return;
        File.WriteAllText(_savePath, JsonSerializer.Serialize(this, JsonOptions));
    }

    public void OnRunEnd(long startTime)
    {
        if (!_profileId.HasValue || _saveFile == null || _savePath == null || !File.Exists(_savePath)) return;
        
        File.WriteAllText(_savePath, JsonSerializer.Serialize(this, JsonOptions));

        var finalPath = FileUtils.GetHistoryPath(_profileId.Value, startTime, _saveFile);
        if (finalPath == null) return;
        
        File.Move(_savePath, finalPath, overwrite: true);
    }
}