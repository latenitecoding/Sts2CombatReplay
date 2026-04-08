using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode;

public class CombatReplayDb
{
    private static JsonSerializerOptions _jsonOptions = new() { 
        WriteIndented = true,
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    public static CombatReplayDb? LoadFromFile(int? profileId)
    {
        var savePath = GetSavePath(profileId);
        if (File.Exists(savePath))
        {
            return JsonSerializer.Deserialize<CombatReplayDb>(File.ReadAllText(savePath));
        }

        return null;
    }
    
    public string? RunSeed { get; set; }
    public int CurrentAct { get; set; }
    public int CurrentRoom { get; set; }
    public int CurrentCombat { get; set; }
    public int CurrentTurn { get; set; }
    private bool _inCombat;
    
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
    public int TotalCardsCreated { get; set; }
    public int TotalSummoned { get; set; }
    public int TotalOrbsChanneled { get; set; }
    public int TotalOrbsEvoked { get; set; }

    private int _currentTurnDamage;
    private int _currentTurnBlock;
    public int BestSingleTurnDamage { get; set; }
    public int BestSingleTurnBlock { get; set; }

    public int TotalStrengthGained { get; set; }
    public int TotalVulnerableApplied { get; set; }
    public int TotalWeakApplied { get; set; }
    public int TotalPoisonApplied { get; set; }
    public int TotalDoomApplied { get; set; }
    
    public Decimal AvgStrengthGainedPerCombat { get; set; }
    public Decimal AvgVulnerableAppliedPerCombat { get; set; }
    public Decimal AvgWeakAppliedPerCombat { get; set; }
    public Decimal AvgPoisonAppliedPerCombat { get; set; }
    public Decimal AvgDoomAppliedPerCombat { get; set; }
    
    private string _prevCardPlay = "";
    private int _currentAttackDamage;
    private int _currentDefenseBlock;
    public int BestSingleDamage { get; set; }
    public int BestSingleBlock { get; set; }

    public Dictionary<string, CardStats> BestAttack { get; init; } = [];
    public Dictionary<string, CardStats> BestDefend { get; init; } = [];
    public Dictionary<string, CardStats> MostPlayedCard { get; init; } = [];

    private CombatStats _currentCombat = new CombatStats() { Enemies = [] };
    public CombatStats? HeroicCombat { get; set; }
    public CombatStats? NemesisCombat { get; set; }
    public List<CombatStats> Combats { get; init; } = [];

    private List<Creature> _currentCreatures = [];

    public void NextAct() => CurrentAct += 1;
    public void NextRoom() => CurrentRoom += 1;
    
    public void NextTurn()
    {
        CurrentTurn += 1;
        _currentCombat.TotalTurns += 1;
        SetBestCardTurnStats();
    }

    public void StartCombat()
    {
        CurrentCombat += 1;
        CurrentTurn = 0;
        _inCombat = true;

        _currentCombat.CombatId = CurrentCombat;
    }

    public void EndCombat()
    {
        _inCombat = false;
        
        SetBestCardTurnStats();
        SetAverages();
        RecordCombat();
        
        _currentCreatures.Clear();
        _currentCombat = new CombatStats() { Enemies = [] };
    }
    
    public bool IsInCombat() => _inCombat;

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
        AvgDamagePerTurn = Math.Round(((decimal)TotalDamage) / TotalTurnsPlayed, 2);
        AvgBlockPerTurn = Math.Round(((decimal)TotalBlockGained) / TotalTurnsPlayed, 2);

        AvgDamagePerCombat = Math.Round(((decimal)TotalDamage) / CurrentCombat, 2);
        AvgBlockPerCombat = Math.Round(((decimal)TotalBlockGained) / CurrentCombat, 2);
        AvgDamageReceivedPerCombat = Math.Round(((decimal)TotalDamageReceived) / CurrentCombat, 2);
        AvgTrueDamageReceivedPerCombat = Math.Round(((decimal)TotalTrueDamageReceived) / CurrentCombat, 2);
        AvgBlockedDamageReceivedPerCombat = Math.Round(((decimal)TotalBlockedDamageReceived) / CurrentCombat, 2);

        AvgStrengthGainedPerCombat = Math.Round(((decimal)TotalStrengthGained) / CurrentCombat, 2);
        AvgVulnerableAppliedPerCombat = Math.Round(((decimal)TotalVulnerableApplied) / CurrentCombat, 2);
        AvgWeakAppliedPerCombat = Math.Round(((decimal)TotalWeakApplied) / CurrentCombat, 2);
        AvgPoisonAppliedPerCombat = Math.Round(((decimal)TotalPoisonApplied) / CurrentCombat, 2);
        AvgDoomAppliedPerCombat = Math.Round(((decimal)TotalDoomApplied) / CurrentCombat, 2);
    }

    public void RecordCombat()
    {
        Combats.Add(_currentCombat);
        if (HeroicCombat == null || _currentCombat.TotalDamageDealt > HeroicCombat.TotalDamageDealt)
        {
            HeroicCombat = _currentCombat;
        }
        if (NemesisCombat == null || _currentCombat.TotalTrueDamageReceived > NemesisCombat.TotalTrueDamageReceived)
        {
            NemesisCombat = _currentCombat;
        }
    }

    public void AddCombatCreature(Creature creature, string fmtTitle)
    {
        _currentCreatures.Add(creature);
        if (creature.IsEnemy)
        {
            _currentCombat.Enemies.Add(fmtTitle);
        }
    }
    
    public IReadOnlyList<Creature> GetCombatCreatureList() => _currentCreatures;

    public Dictionary<string, CardStats> CardPlayStats { get; init; } = new();

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
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TimesPlayed++;

        if (MostPlayedCard.Count == 0 || cardStats.TimesPlayed > MostPlayedCard.Values.First().TimesPlayed)
        {
            MostPlayedCard.Clear();
            MostPlayedCard[TitleToKey(cardTitle)] = cardStats;
        }
        
        BestSingleDamage = Math.Max(BestSingleDamage, _currentAttackDamage);
        BestSingleBlock = Math.Max(BestSingleBlock, _currentDefenseBlock);
        _currentAttackDamage = 0;
        _currentDefenseBlock = 0;
        
        _prevCardPlay = cardTitle;
    }

    public void AddDamageDealt(string cardTitle, int amount)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TotalDamageDealt += amount;

        if (BestAttack.Count == 0 || cardStats.TotalDamageDealt > BestAttack.Values.First().TotalDamageDealt)
        {
            BestAttack.Clear();
            BestAttack[TitleToKey(cardTitle)] = cardStats;
        }

        _currentTurnDamage += amount;
        if (cardTitle == _prevCardPlay)
        {
            _currentAttackDamage += amount;
        }
    }

    public void AddCombatDamageDealt(int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamage += totalDamage;
        _currentCombat.TotalDamageDealt += totalDamage;
        if (trueDamage.HasValue)
        {
            TotalTrueDamage += trueDamage.Value;
            _currentCombat.TotalTrueDamageDealt += trueDamage.Value;
        }
        if (blockedDamage.HasValue)
        {
            TotalBlockedDamage += blockedDamage.Value;
            _currentCombat.TotalBlockedDamageDealt += blockedDamage.Value;
        }
    }

    public void AddCombatDamageReceived(int totalDamage, int? trueDamage, int? blockedDamage)
    {
        TotalDamageReceived += totalDamage;
        _currentCombat.TotalDamageReceived += totalDamage;
        if (trueDamage.HasValue)
        {
            TotalTrueDamageReceived += trueDamage.Value;
            _currentCombat.TotalTrueDamageReceived += trueDamage.Value;
        }
        if (blockedDamage.HasValue)
        {
            TotalBlockedDamageReceived += blockedDamage.Value;
            _currentCombat.TotalBlockedDamageReceived += blockedDamage.Value;
        }                  
    }

    public void AddBlockGained(string cardTitle, int amount)
    {
        var cardStats = GetOrCreateCardStats(cardTitle);
        cardStats.TotalBlockGained += amount;

        if (BestDefend.Count == 0 || cardStats.TotalBlockGained > BestDefend.Values.First().TotalBlockGained)
        {
            BestDefend.Clear();
            BestDefend[TitleToKey(cardTitle)] = cardStats;
        }

        _currentTurnBlock += amount;
        if (cardTitle == _prevCardPlay)
        {
            _currentDefenseBlock += amount;
        }
    }

    public void AddCombatBlockGained(int amount)
    {
        TotalBlockGained += amount;
        _currentCombat.TotalBlockGained += amount;
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
            if (File.Exists(savePath) && finalPath != null)
            {
                File.Move(savePath, finalPath, overwrite: true);
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

    public class CombatStats
    {
        public int CombatId { get; set; }
        public required List<string> Enemies { get; init; }
        
        public int TotalTurns { get; set; }
        
        public int TotalDamageDealt { get; set; }
        public int TotalTrueDamageDealt { get; set; }
        public int TotalBlockedDamageDealt { get; set; }
        
        public int TotalBlockGained { get; set; }
        
        public int TotalDamageReceived { get; set; }
        public int TotalTrueDamageReceived { get; set; }
        public int TotalBlockedDamageReceived { get; set; }
    }
}