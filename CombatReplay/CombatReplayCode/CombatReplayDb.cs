using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace CombatReplay.CombatReplayCode;

public class CombatReplayDb
{
    public static CombatReplayDb? LoadFromFile(int? profileId)
    {
        var savePath = GetSavePath(profileId);
        if (File.Exists(savePath))
        {
            return JsonSerializer.Deserialize<CombatReplayDb>(File.ReadAllText(savePath));
        }

        return null;
    }
    
    public int CurrentAct { get; set; }
    public int CurrentRoom { get; set; }
    public int CurrentCombat { get; set; }
    public int CurrentTurn { get; set; }
    public bool InCombat { get; set; }

    private Decimal _currentTurnDamage;
    private Decimal _currentTurnBlock;
    public Decimal BestSingleTurnDamage { get; set; }
    public Decimal BestSingleTurnBlock { get; set; }

    private string _prevCardPlay = "";
    private Decimal _currentAttackDamage;
    private Decimal _currentDefenseBlock;
    public Decimal BestSingleDamage { get; set; }
    public Decimal BestSingleBlock { get; set; }

    public void NextAct() => CurrentAct += 1;
    public void NextRoom() => CurrentRoom += 1;
    
    public void NextTurn()
    {
        CurrentTurn += 1;
        SetBestCardTurnStats();
    }

    public void StartCombat()
    {
        CurrentCombat += 1;
        CurrentTurn = 0;
        InCombat = true;
    }

    public void EndCombat()
    {
        InCombat = false;
        SetBestCardTurnStats();
    }

    private void SetBestCardTurnStats()
    {
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);
        BestSingleTurnDamage = Math.Max(BestSingleTurnDamage, _currentTurnDamage);
        BestSingleTurnBlock = Math.Max(BestSingleTurnBlock, _currentTurnBlock);

        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
        _currentTurnDamage = 0;
        _currentTurnBlock = 0;
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
        
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);
        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
        
        _prevCardPlay = cardTitle;
    }

    public void AddDamageDealt(string cardTitle, Decimal amount)
    {
        GetOrCreateCardStats(cardTitle).TotalDamageDealt += amount;

        _currentTurnDamage += amount;
        if (cardTitle == _prevCardPlay)
        {
            _currentAttackDamage += amount;
        }
    }

    public void AddBlockGained(string cardTitle, Decimal amount)
    {
        GetOrCreateCardStats(cardTitle).TotalBlockGained += amount;

        _currentTurnBlock += amount;
        if (cardTitle == _prevCardPlay)
        {
            _currentDefenseBlock += amount;
        }
    }

    public void InProgressSave(int? profileId)
    {
        File.WriteAllText(GetSavePath(profileId), JsonSerializer.Serialize(this));
    }

    public void SaveRun(int? profileId, long startTime)
    {
        var savePath = GetSavePath(profileId);
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
    
    private static string GetSavePath(int? profileId)
    {
        var backupPath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_stats_current.json"
        );

        if (!profileId.HasValue)
        {
            return backupPath;
        }
        
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId.Value}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "sts2_combat_stats_current.json"
            ))
            .FirstOrDefault(backupPath);
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
                $"sts2_combat_stats_{startTime}.json"
            ))
            .FirstOrDefault();
    }

    public class CardStats
    {
        public int TimesPlayed { get; set; }
        public Decimal TotalDamageDealt { get; set; }
        public Decimal TotalBlockGained { get; set; }
    }
}