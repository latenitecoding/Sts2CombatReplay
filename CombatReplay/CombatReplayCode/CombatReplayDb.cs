using System.Text.Json;
using System.Text.RegularExpressions;
using Godot;
using MegaCrit.Sts2.Core.Entities.Creatures;

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

    private CombatStats _currentCombat = new();
    public CombatStats? HeroicCombat { get; set; }
    public CombatStats? NemesisCombat { get; set; }
    public List<CombatStats> Combats { get; set; } = new();

    private List<Creature> _currentCreatures = new();

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

        _currentCombat = new CombatStats()
        {
            CombatId = CurrentCombat
        };
    }

    public void EndCombat()
    {
        _inCombat = false;
        
        SetBestCardTurnStats();
        SetAverages();
        RecordCombat();
        
        _currentCreatures.Clear();
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
        AvgDamagePerTurn = ((decimal) TotalDamage) / TotalTurnsPlayed;
        AvgBlockPerTurn = ((decimal) TotalBlockGained) / TotalTurnsPlayed;
        
        AvgDamagePerCombat = ((decimal) TotalDamage) / CurrentCombat;
        AvgBlockPerCombat = ((decimal) TotalBlockGained) / CurrentCombat;
        AvgDamageReceivedPerCombat = ((decimal) TotalDamageReceived) / CurrentCombat;
        AvgTrueDamageReceivedPerCombat = ((decimal) TotalTrueDamageReceived) / CurrentCombat;
        AvgBlockedDamageReceivedPerCombat = ((decimal) TotalBlockedDamageReceived) / CurrentCombat;

        AvgStrengthGainedPerCombat = ((decimal) TotalStrengthGained) / CurrentCombat;
        AvgVulnerableAppliedPerCombat = ((decimal) TotalVulnerableApplied) / CurrentCombat;
        AvgWeakAppliedPerCombat = ((decimal) TotalWeakApplied) / CurrentCombat;
        AvgPoisonAppliedPerCombat = ((decimal) TotalPoisonApplied) / CurrentCombat;
        AvgDoomAppliedPerCombat = ((decimal) TotalDoomApplied) / CurrentCombat;
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

    public void AddCombatCreature(Creature creature)
    {
        MainFile.Logger.Info($"Adding creature {creature.Name}");
        
        _currentCreatures.Add(creature);
        _currentCreatures.Sort((c1, c2) => {
            if (c1.CombatId.HasValue && c2.CombatId.HasValue)
            {
                return (int) (c1.CombatId.Value - c2.CombatId.Value);
            }
            if (c1.CombatId.HasValue)
            {
                return -1;
            }
            if (c2.CombatId.HasValue)
            {
                return 1;
            }
            return 0;
        });
    }
    
    public IReadOnlyList<Creature> GetCombatCreatureList() => _currentCreatures;

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
        GetOrCreateCardStats(cardTitle).TotalBlockGained += amount;

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