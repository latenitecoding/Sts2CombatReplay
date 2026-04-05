using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.MonsterMoveStateMachine;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Saves;
using MegaCrit.Sts2.Core.ValueProps;

namespace CombatReplay.CombatReplayCode;

public class CombatActionTracker : AbstractModel
{
    // Required by AbstractModel; used for hooking into ModHelper
    public override bool ShouldReceiveCombatHooks => true;
    
    private readonly ConcurrentQueue<string> _queue = new();
    private Task? _writerTask;
    private CancellationTokenSource? _cts;

    private int? _profileId;
    private string? _savePath;
    
    private CombatReplayDb _db = new();
    
    private void StartTracking()
    {
        MainFile.Logger.Info($"CombatReplay tracking started");
        
        var saveManager = SaveManager.Instance;
        
        MainFile.Logger.Info($"SaveManager Profile ID {saveManager.CurrentProfileId}");
        MainFile.Logger.Info($"SaveManager Has Save: {saveManager.HasRunSave}");
        
        _profileId =  saveManager.CurrentProfileId;
        _savePath = GetSavePath(saveManager.CurrentProfileId);
        MainFile.Logger.Info($"CombatReplay save path set to `{_savePath}`");

        if (saveManager.HasRunSave)
        {
            MainFile.Logger.Info($"Using existing save @ `{_savePath}`");
            _db = CombatReplayDb.LoadFromFile(_profileId) ?? _db;
        }
        else if (_savePath != null)
        {
            MainFile.Logger.Info($"Overwriting existing save @ `{_savePath}`");
            File.Create(_savePath).Close();
        }

        _cts = new CancellationTokenSource();
        _writerTask = Task.Run(() => WriterLoop(_cts.Token));
    }

    private async Task StopTracking()
    {
        if (_writerTask == null) return;
        if (_cts != null) await _cts.CancelAsync();
        await _writerTask;
        _writerTask = null;
        MainFile.Logger.Info($"CombatReplay tracking stopped");
    }

    private async Task WriterLoop(CancellationToken ct)
    {
        try
        {
            if (_savePath != null)
            {
                await using var writer = new StreamWriter(_savePath, append: true);
                while (!ct.IsCancellationRequested)
                {
                    while (_queue.TryDequeue(out var entry))
                    {
                        await writer.WriteLineAsync(entry);
                    }

                    await writer.FlushAsync(ct);
                    await Task.Delay(100, ct);
                }
            }
        }
        catch (OperationCanceledException)
        {
            MainFile.Logger.Info("WriterLoop operation cancelled");
        }
        finally
        {
            MainFile.Logger.Info("Draining entries remaining in queue");

            if (_savePath != null)
            {
                await using var writer = new StreamWriter(_savePath, append: true);
                while (_queue.TryDequeue(out var entry))
                {
                    await writer.WriteLineAsync(entry);
                }
            }
        }
    }
    
    public void OnRunStarted()
    {
        StartTracking();
        
        WriteIt($"# Run starting #");
        AfterActEntered();
    }

    public override Task AfterActEntered()
    {
        _db.NextAct();
        WriteIt($"### Act {_db.CurrentAct} started ###");
        
        // each act starts with an 'Ancient' room which is handled implicitly by the game
        _db.NextRoom();
        WriteIt($"##### Room: {_db.CurrentRoom} (`Ancient`) **entering** #####");
        
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        WriteIt($"##### Room: {_db.CurrentRoom} (`{room.RoomType.ToString()}`) **entering** #####");
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _db.StartCombat();
        WriteIt($"=== Combat: {_db.CurrentCombat} **starting** ===");
        
        return Task.CompletedTask;
    }

    public Task OnCreatureAdded(Creature creature)
    {
        var designation = (creature.IsEnemy)
            ? "Enemy"
            : ((creature.IsPet)
                ? "Pet"
                : ((creature.IsPlayer)
                    ? "Player"
                    : "Other"));
        WriteIt($"> {designation}: {FormatCreature(creature)} **present** <\\");
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        _db.NextTurn();
        WriteIt($"Turn: {_db.CurrentTurn} **started**");
        
        return Task.CompletedTask;       
    }

    public override Task AfterOstyRevived(Creature osty)
    {
        WriteIt($"> {FormatCreature(osty)} **revived** <\\");
        return Task.CompletedTask;
    }

    public override Task AfterEnergyReset(Player player)
    {
        WriteIt($"> {FormatPlayer(player)} **reset** `{player.MaxEnergy}` energy <\\");
        return Task.CompletedTask;
    }
    
    public override Task AfterStarsGained(int amount, Player gainer)
    {
        WriteIt($"> {FormatPlayer(gainer)} **gained** `{amount}` stars <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        var keywords = string.Join(", ", card.Keywords.Select(keyword => $"`{keyword}`"));
        var tags = string.Join(", ", card.Tags.Select(tag => $"`{tag}`"));

        var enchantment= card.Enchantment?.Title.ToString() ?? "";
        var affliction = card.Affliction?.Title.ToString() ?? "";
        
        var energyCost = (card.EnergyCost.CostsX) ? "X" : card.EnergyCost.Canonical.ToString();
        var starCost = (card.HasStarCostX) ? "X" : card.CurrentStarCost.ToString();

        if (card.CurrentStarCost > 0 || card.HasStarCostX)
        {
            WriteIt($"> **drew** {FormatCard(card)} [{tags}] [{keywords}] [{enchantment}] [{affliction}] **costing** `{energyCost}` energy **and** `{starCost}` stars <\\");
        }
        else
        {
            WriteIt($"> **drew** {FormatCard(card)} [{tags}] [{keywords}] [{enchantment}] [{affliction}] **costing** `{energyCost}` energy <\\");
        }
        
        return Task.CompletedTask;
    }

    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        WriteIt($"> {FormatCard(card)} **cost** `{amount}` energy <\\");
        return Task.CompletedTask;
    }

    public override Task AfterStarsSpent(int amount, Player spender)
    {
        WriteIt($"> {FormatPlayer(spender)} **spent** `{amount}` stars <\\");
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        if (target != null)
        {
            WriteIt($"> _**used** `{potion.Title}` **on** {FormatCreature(target)}_ <\\");
        }
        else
        {
            WriteIt($"> _**used** `{potion.Title}`_ <\\");
        }

        return Task.CompletedTask;
    }

    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        WriteIt($"> _**discarded** `{potion.Title}`_ <\\");
        return Task.CompletedTask;
    }

    public override Task AfterForge(Decimal amount, Player forger, AbstractModel? source)
    {
        WriteIt($"> {FormatPlayer(forger)} **forged** `{amount}` <\\");
        return Task.CompletedTask;
    }

    public override Task AfterSummon(PlayerChoiceContext ctx, Player summoner, Decimal amount)
    {
        WriteIt($"> {FormatPlayer(summoner)} **summoned** `{amount}` <\\");
        return Task.CompletedTask;
    }

    public void RecordCardPlayed(PlayCardAction action)
    {
        var card = action.NetCombatCard.ToCardModel();
        if (action.Target != null)
        {
            WriteIt($"> _{FormatPlayer(action.Player)} **played** {FormatCard(card)} **targeting** {FormatCreature(action.Target)}_ <\\");
        }
        else
        {
            WriteIt($"> _{FormatPlayer(action.Player)} **played** {FormatCard(card)}_ <\\");
        }
        
        _db.AddCardPlay(card.Title);
    }

    public override Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
    {
        WriteIt($"> {FormatPlayer(player)} **channeled** `{orb.Title}` <\\");
        return Task.CompletedTask;
    }

    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        WriteIt($"> `{orb.Title}` **evoked** <\\");
        return Task.CompletedTask;
    }
    
    public void RecordEnemyIntent(Creature owner, MoveState state)
    {
        if (!_db.InCombat) return;
        
        var intentions = string.Join(", ", state.Intents.Select(intention => $"`{intention.IntentType.ToString()}`"));
        WriteIt($"> {FormatCreature(owner)} **intends** [{intentions}] <\\");
    }
    
    public override Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, Decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **using** `{cardSource.Title}` **against** {FormatCreature(target)} **totaling** `{amount}` dmg <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **hitting** {FormatCreature(target)} **totaling** `{amount}` dmg <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` **targeting** {FormatCreature(target)} **totaling** `{amount}` dmg <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **receiving** `{amount}` dmg <\\");
        }

        if (cardSource != null)
        {
            _db.AddDamageDealt(cardSource.Title, amount);
        }
        return Task.CompletedTask;
    }
    
    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` **against** {FormatCreature(target)} **dealing** `{result.TotalDamage}` dmg **removing** `{result.BlockedDamage}` blk <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **hit** {FormatCreature(target)} **dealing** `{result.TotalDamage}` dmg **removing** `{result.BlockedDamage}` blk <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` **targeted** {FormatCreature(target)} **dealing** `{result.TotalDamage}` dmg **removing** `{result.BlockedDamage}` blk <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **received** `{result.TotalDamage}` dmg **removing** `{result.BlockedDamage}` blk <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterBlockBroken(Creature creature)
    {
        WriteIt($"> {FormatCreature(creature)} **broken** <\\");
        return Task.CompletedTask;
    }

    public override Task AfterDeath(PlayerChoiceContext ctx, Creature creature, bool wasRemovalPrevented,
        float deathAnimLength)
    {
        WriteIt($"> {FormatCreature(creature)} **defeated** <\\");
        return Task.CompletedTask; 
    }

    public override Task AfterBlockGained(Creature creature, Decimal amount, ValueProp props, CardModel? cardSource)
    {
        if (cardSource != null)
        {
            WriteIt($"> {FormatCreature(creature)} **used** `{cardSource.Title}` **gaining** `{amount}` blk <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(creature)} **gained** `{amount}` blk <\\");
        }

        if (cardSource != null)
        {
            _db.AddBlockGained(cardSource.Title, amount);
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardRetained(CardModel card)
    {
        WriteIt($"> **retained** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        WriteIt($"> **discarded** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        WriteIt($"> **exhausted** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterTurnEnd(PlayerChoiceContext ctx, CombatSide side)
    {
        if (side == CombatSide.Player)
        {
            WriteIt("> Player phase **ended** <\\");
        }
        else if (side == CombatSide.Enemy)
        {
            WriteIt($"> Turn: {_db.CurrentTurn} **ended** <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterHandEmptied(PlayerChoiceContext ctx, Player player)
    {
        WriteIt($"> {FormatPlayer(player)} **emptied** `hand` <\\");
        return Task.CompletedTask;
    }

    public override Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)
    {
        WriteIt($"> {FormatPlayer(shuffler)} **shuffled** <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCombatEnd(CombatRoom room)
    {
        _db.EndCombat();
        WriteIt($"=== Combat: {_db.CurrentCombat} **ended** ===\\");
    
        MainFile.Logger.Info($"CombatReplay logging stats for combat {_db.CurrentCombat}");
        _db.InProgressSave(_profileId);
        
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        WriteIt($"=== Combat: {_db.CurrentCombat} **was** `victory` ===\\");
        return Task.CompletedTask;
    }
    
    // current firing when a room is entered (except the first room)
    public void OnRoomExited()
    {
        WriteIt($"##### Room: {_db.CurrentRoom} **exited** #####");
        
        MainFile.Logger.Info($"CombatReplay logging stats for room {_db.CurrentRoom}");
        _db.InProgressSave(_profileId);
        
        _db.NextRoom();
        WriteIt($"##### Room: {_db.CurrentRoom} **entering** #####");
    }

    public void OnRunEnded(long startTime)
    {
        WriteIt($"=== Run Ended ===");
        TaskHelper.RunSafely(FinalizeHistory(startTime));
        _db.SaveRun(_profileId, startTime);
    }

    private async Task FinalizeHistory(long startTime)
    {
        await StopTracking();

        if (_profileId.HasValue)
        {
            var finalPath = GetHistoryPath(_profileId.Value, startTime);
            if (_savePath != null && finalPath != null)
            {
                File.Move(_savePath, finalPath);
                MainFile.Logger.Info($"Combat history saved to: {finalPath}");
            }
        }       
    }

    private static string FormatCard(CardModel card)
    {
        return $"`{card.Title}`";
    }

    private static string FormatCreature(Creature creature)
    {
        return (creature.CombatId != null)
            ? $"`{creature.Name}` (`{creature.CombatId}`) [`{creature.Block}|{creature.CurrentHp}/{creature.MaxHp}` bHP]"
            : $"`{creature.Name}` (`{creature.ModelId}`) [`{creature.Block}|{creature.CurrentHp}/{creature.MaxHp}` bHP]";
    }

    private static string FormatPlayer(Player player)
    {
        return (player.Creature.CombatId != null)
            ? $"Player: `{player.Character.Title.GetFormattedText()}` (`{player.Creature.CombatId}`)"
            : $"Player: `{player.Character.Title.GetFormattedText()}` (`{player.NetId}`)";
    }
    
    private static string GetSavePath(int? profileId)
    {
        var backupPath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_tracker_current.replay"
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
                "sts2_combat_tracker_current.replay"
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
                $"sts2_combat_tracker_{startTime}.replay"
            ))
            .FirstOrDefault();
    }
    
    private void WriteIt(string msg)
    {
        if (_writerTask != null)
        {
            _queue.Enqueue(msg);
        }
    }
}