using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;

namespace CombatReplay.CombatReplayCode;

public class CombatReplayDb
{
    private static JsonSerializerOptions _jsonOptions = new() { WriteIndented = true };
    
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
    
    public int TotalTurnsPlayed { get; set; }
    public int TotalEnemiesFought { get; set; }
    public int TotalPotionsUsed { get; set; }
    public int TotalPotionsDiscarded { get; set; }
    
    public int TotalDamage { get; set; }
    public int TotalTrueDamage { get; set; }
    public int TotalBlockedDamage { get; set; }
    public int TotalHpHealed { get; set; }
    
    public int TotalBlockGained { get; set; }
    public int TotalDamageReceived { get; set; }
    public int TotalTrueDamageReceived { get; set; }
    public int TotalBlockedDamageReceived { get; set; }
    public int TotalSelfDamage { get; set; }
    
    public int TotalPetDamage { get; set; }
    public int TotalPetDamageReceived { get; set; }
    
    public int TotalAnonymousDamage { get; set; }
    public int TotalAnonymousBlock { get; set; }
    
    public Decimal AvgDamagePerTurn { get; set; }
    public Decimal AvgBlockPerTurn { get; set; }
    public Decimal AvgDamagePerCombat { get; set; }
    public Decimal AvgBlockPerCombat { get; set; }
    public Decimal AvgDamageReceivedPerCombat { get; set; }
    public Decimal AvgTrueDamageReceivedPerCombat { get; set; }
    public Decimal AvgBlockedDamageReceivedPerCombat { get; set; }
    
    public int TotalEnergyGained { get; set; }
    public int TotalStarsGained { get; set; }
    public int TotalEnergySpent { get; set; }
    public int TotalStarsSpent { get; set; }
    
    public int TotalCardsDrawn { get; set; }
    public int TotalCardsPlayed { get; set; }
    public int TotalCardsDiscarded { get; set; }
    public int TotalCardsRetained { get; set; }
    public int TotalCardsExhausted { get; set; }
    public int TotalEmptyHands { get; set; }
    public int TotalDeckShuffles { get; set; }
    
    public int TotalOstyRevives { get; set; }
    public int TotalForged { get; set; }
    public int TotalSummoned { get; set; }
    public int TotalOrbsChanneld { get; set; }
    public int TotalOrbsEvoked { get; set; }

    private int _currentTurnDamage;
    private int _currentTurnBlock;
    public int BestSingleTurnDamage { get; set; }
    public int BestSingleTurnBlock { get; set; }

    private string _prevCardPlay = "";
    private int _currentAttackDamage;
    private int _currentDefenseBlock;
    public int BestSingleDamage { get; set; }
    public int BestSingleBlock { get; set; }

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
        SetAverages();
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

    private void SetAverages()
    {
        AvgDamagePerTurn = ((decimal) TotalDamage) / TotalTurnsPlayed;
        AvgBlockPerTurn = ((decimal) TotalBlockGained) / TotalTurnsPlayed;
        
        AvgDamagePerCombat = ((decimal) TotalDamage) / CurrentCombat;
        AvgBlockPerCombat = ((decimal) TotalBlockGained) / CurrentCombat;
        AvgDamageReceivedPerCombat = ((decimal) TotalDamageReceived) / CurrentCombat;
        AvgTrueDamageReceivedPerCombat = ((decimal) TotalTrueDamageReceived) / CurrentCombat;
        AvgBlockedDamageReceivedPerCombat = ((decimal) TotalBlockedDamageReceived) / CurrentCombat;
    }

    public Dictionary<string, CardStats> CardPlayStats { get; set; } = new();

    private static string TitleToKey(string cardTitle)
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

    public void AddDamageDealt(string cardTitle, int amount)
    {
        GetOrCreateCardStats(cardTitle).TotalDamageDealt += amount;

        _currentTurnDamage += amount;
        if (cardTitle == _prevCardPlay)
        {
            _currentAttackDamage += amount;
        }
    }

    public void AddBlockGained(string cardTitle, int amount)
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
        File.WriteAllText(GetSavePath(profileId), JsonSerializer.Serialize(this, _jsonOptions));
    }

    public void SaveRun(int? profileId, long startTime)
    {
        var savePath = GetSavePath(profileId);
        File.WriteAllText(savePath, JsonSerializer.Serialize(this, _jsonOptions));

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
        var destDir = Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "combat_history"
            ))
            .FirstOrDefault();
        if (destDir == null)
        {
            return null;
        }
        Directory.CreateDirectory(destDir);
        return Path.Combine(destDir, $"sts2_combat_stats_{startTime}.json");
    }

    public class CardStats
    {
        public int TimesPlayed { get; set; }
        public Decimal TotalDamageDealt { get; set; }
        public Decimal TotalBlockGained { get; set; }
    }
}