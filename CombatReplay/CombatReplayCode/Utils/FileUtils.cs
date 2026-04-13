using Godot;

namespace CombatReplay.CombatReplayCode.Utils;

public static class FileUtils
{
    public static string? GetHistoryPath(int profileId, long startTime, string saveFile)
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        var destDir = Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "combat_history"
            ))
            .FirstOrDefault();
        if (destDir == null) return null;
        Directory.CreateDirectory(destDir);
        return Path.Combine(destDir, WithStartTime(saveFile, startTime));
    }

    public static string GetSavePath(int profileId, bool isMultiplayer, string saveFile, string multiRunId) 
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                isMultiplayer ? AsMultiplayer(saveFile, multiRunId) : saveFile
            ))
            .FirstOrDefault(GetDefaultSavePath(isMultiplayer, saveFile));
    }

    private static string AsMultiplayer(string saveFile, string multiRunId)
    {
        return $"{Path.GetFileNameWithoutExtension(saveFile)}_{multiRunId}{Path.GetExtension(saveFile)}";
    }

    private static string WithStartTime(string saveFile, long startTime)
    {
        return $"{Path.GetFileNameWithoutExtension(saveFile)}_{startTime}{Path.GetExtension(saveFile)}";
    }
    
    private static string GetDefaultSavePath(bool isMultiplayer, string saveFile)
    {
        return Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            isMultiplayer ? AsMultiplayer(saveFile, "mp") : saveFile);
    }
}