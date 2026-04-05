using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace CombatReplay.CombatReplayCode;

public class CombatReplayDb
{
    public static CombatReplayDb? LoadFromFile(int? profileId)
    {
        var savePath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_replay_current.md"
        );
        if (profileId.HasValue)
        {
            savePath = GetSavePath(profileId.Value) ?? savePath;
        }
        return JsonSerializer.Deserialize<CombatReplayDb>(File.ReadAllText(savePath));
    }
    
    public int CurrentAct { get; private set; }
    public int CurrentRoom { get; private set; }
    public int CurrentCombat { get; private set; }
    public int CurrentTurn { get; private set; }
    public bool InCombat { get; private set; }

    public void NextAct() => CurrentAct += 1;
    public void NextRoom() => CurrentRoom += 1;
    public void NextTurn() => CurrentTurn += 1;
    public void EndCombat() => InCombat = false;
   
    public void StartCombat()
    {
        CurrentCombat += 1;
        CurrentTurn = 0;
        InCombat = true;
    }

    public Dictionary<string, CardStats> CardPlayStats { get; set; } = new();

    private string TitleToKey(string cardTitle)
    {
        return Regex.Replace(cardTitle, @"\+\d*$", "").Trim();
    }
    
    private CardStats GetOrCreateCardStats(string cardTitle)
    {
        cardTitle = TitleToKey(cardTitle);
        if (!CardPlayStats.TryGetValue(cardTitle, out var stats))
        {
            stats = new CardStats();
            CardPlayStats[cardTitle] = stats;
        }
        return stats;
    }

    public void AddCardPlay(string cardTitle)
    {
        GetOrCreateCardStats(cardTitle).TimesPlayed++;
    }

    public void AddDamageDealt(string cardTitle, int damageDealt)
    {
        GetOrCreateCardStats(cardTitle).TotalDamageDealt += damageDealt;
    }

    public void AddBlockGained(string cardTitle, int blockGained)
    {
        GetOrCreateCardStats(cardTitle).TotalBlockGained += blockGained;
    }

    public void InProgressSave(int? profileId)
    {
        var savePath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_replay_current.md"
        );
        if (profileId.HasValue)
        {
            savePath = GetSavePath(profileId.Value) ?? savePath;
        }
        File.WriteAllText(savePath, JsonSerializer.Serialize(this));
    }

    public void SaveRun(int? profileId, long startTime)
    {
        var savePath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_replay_current.md"
        );
        if (profileId.HasValue)
        {
            savePath = GetSavePath(profileId.Value) ?? savePath;
        }
        File.WriteAllText(savePath, JsonSerializer.Serialize(this));

        if (profileId.HasValue)
        {
            var finalPath = GetHistoryPath(profileId.Value, startTime);
            if (finalPath != null)
            {
                File.Move(savePath, finalPath);
            }           
        }
    }
    
    private static string? GetSavePath(int profileId)
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "sts2_combat_replay_stats_current.json"
            ))
            .FirstOrDefault();
    }
    
    private static string? GetHistoryPath(int profileId, long startTime)
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "combat_history",
                $"sts2_combat_replay_stats_{startTime}.json"
            ))
            .FirstOrDefault();
    }

    public class CardStats
    {
        public int TimesPlayed { get; set; }
        public int TotalDamageDealt { get; set; }
        public int TotalBlockGained { get; set; }
    }
}