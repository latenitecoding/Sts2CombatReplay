using Godot;

namespace CombatReplay.CombatReplayCode.Utils;

public static class FileUtils
{
    public static string GetSavePath(int profileId, bool isMultiplayer, string saveFile) 
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                isMultiplayer ? AsMultiplayer(saveFile) : saveFile
            ))
            .FirstOrDefault(GetDefaultSavePath(isMultiplayer, saveFile));
    }
    
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

    private static string AsMultiplayer(string saveFile)
    {
        var split = saveFile.Split(".");
        return $"{split[0]}_mp.{split[1]}";
    }

    private static string WithStartTime(string saveFile, long startTime)
    {
        var split = saveFile.Split(".");
        return $"{split[0]}_{startTime}.{split[1]}";
    }
    
    private static string GetDefaultSavePath(bool isMultiplayer, string saveFile)
    {
        return Path.Combine(ProjectSettings.GlobalizePath("user://"), isMultiplayer ? AsMultiplayer(saveFile) : saveFile);
    }
}