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

    private int _currentAct;
    private int _currentRoom;
    private int _currentCombat;
    private int _currentTurn;

    private bool _startedCombat;

    public void StartTracking()
    {
        MainFile.Logger.Info($"CombatReplay tracking started");
        
        _savePath = Path.Combine(
            ProjectSettings.GlobalizePath("user://"),
            "sts2_combat_replay_current.md"
        );
        _cts = new CancellationTokenSource();
        _writerTask = Task.Run(() => WriterLoop(_cts.Token));

        MainFile.Logger.Info($"CombatReplay save path set to `{_savePath}`");
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
        var saveManager = SaveManager.Instance;
        
        MainFile.Logger.Info($"SaveManager Profile ID {saveManager.CurrentProfileId}");
        MainFile.Logger.Info($"SaveManager Has Save: {saveManager.HasRunSave}");

        _profileId =  saveManager.CurrentProfileId;
        _savePath = GetSavePath(saveManager.CurrentProfileId) ?? _savePath;
        MainFile.Logger.Info($"CombatReplay save path set to `{_savePath}`");

        if (saveManager.HasRunSave)
        {
            MainFile.Logger.Info($"Using existing save @ `{_savePath}`");
        }
        else if (_savePath != null)
        {
            MainFile.Logger.Info($"Overwriting existing save @ `{_savePath}`");
            File.Create(_savePath).Close();
        }
        
        WriteIt($"# Run starting #");
        AfterActEntered();
    }

    public override Task AfterActEntered()
    {
        _currentAct += 1;
        WriteIt($"### Act {_currentAct} started ###");
        
        _currentRoom += 1;
        WriteIt($"##### Room: {_currentRoom} (`Ancient`) **entering** #####");
        
        return Task.CompletedTask;
    }

    public override Task AfterRoomEntered(AbstractRoom room)
    {
        _currentRoom += 1;
        WriteIt($"##### Room: {_currentRoom} (`{room.RoomType.ToString()}`) **entering** #####");
        
        return Task.CompletedTask;
    }

    public override Task BeforeCombatStart()
    {
        _currentCombat += 1;
        _currentTurn = 0;
        
        _startedCombat = true;
        WriteIt($"=== Combat: {_currentCombat} **starting** ===");
        
        return Task.CompletedTask;
    }

    public override Task AfterCreatureAddedToCombat(Creature creature)
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
        _currentTurn += 1;
        WriteIt($"Turn: {_currentTurn} **started**");
        
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
        if (!_startedCombat) return;
        
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
            WriteIt($"> Turn: {_currentTurn} **ended** <\\");
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
        _startedCombat = false;
        WriteIt($"=== Combat: {_currentCombat} **ended* ===");
        return Task.CompletedTask;
    }

    public override Task AfterCombatVictory(CombatRoom room)
    {
        WriteIt($"=== Combat: {_currentCombat} **ended** in `victory` ===");
        return Task.CompletedTask;
    }
    
    public void OnRoomExited()
    {
        WriteIt($"##### Room: {_currentRoom} **exited** #####");
        _currentRoom += 1;
        WriteIt($"##### Room: {_currentRoom} **entering** #####");
    }

    public void OnRunEnded(long startTime)
    {
        WriteIt($"=== Run Ended ===");
        TaskHelper.RunSafely(FinalizeHistory(startTime));
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
    
    private static string? GetSavePath(int profileId)
    {
        var rootPath = Path.Combine(ProjectSettings.GlobalizePath("user://"), "steam");
        return Directory.GetDirectories(rootPath)
            .Select(dir => Path.Combine(rootPath, dir, "modded", $"profile{profileId}"))
            .Where(Directory.Exists)
            .Select(profilePath => Path.Combine(
                profilePath,
                "saves",
                "sts2_combat_tracker_current.md"
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
                $"sts2_combat_tracker_{startTime}.md"
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