using System.Collections.Concurrent;
using Godot;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.Context;
using MegaCrit.Sts2.Core.Entities.Creatures;
using MegaCrit.Sts2.Core.Entities.Players;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.GameActions.Multiplayer;
using MegaCrit.Sts2.Core.Helpers;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.MonsterMoves.Intents;
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
    private bool _loadedSave;
    
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
            _loadedSave = true;
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

        if (_loadedSave) return;
        
        WriteIt($"# Run starting as Player NetId `{LocalContext.NetId}` on Profile{_profileId} #");
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
        if (LocalContext.IsMe(creature))
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <--- THIS IS ME <\\");
        }
        else if (creature is { IsPet: true } && LocalContext.IsMe(creature.PetOwner))
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <--- THIS IS MY PET <\\");
        }
        else
        {
            WriteIt($"> {designation}: {FormatCreature(creature)} **present** <\\");
        }

        _db.AddCombatCreature(creature);
        if (creature.IsEnemy)
        {
            _db.TotalEnemiesFought += 1;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPlayerTurnStart(PlayerChoiceContext ctx, Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        
        _db.NextTurn();
        WriteIt($"Turn: {_db.CurrentTurn} **started**");

        foreach (var creature in _db.GetCombatCreatureList())
        {
            if (creature.IsDead)
            {
                WriteIt($"> {FormatCreature(creature)} **defeated** <\\");
                continue;
            }
            var powers = string.Join(", ", creature.Powers.Select(power => $"`{power.Title.GetFormattedText()} {power.Amount}`"));
            WriteIt($"> {FormatCreature(creature)} **active** **with** [{powers}] powers <\\");
        }

        var pcs = player.PlayerCombatState;
        if (pcs != null)
        {
            var handSize = pcs.Hand.Cards.Count;
            var deckSize = pcs.DrawPile.Cards.Count;
            var discardSize = pcs.DiscardPile.Cards.Count;
            var exhaustSize = pcs.ExhaustPile.Cards.Count;
            WriteIt($"> {FormatPlayer(player)} **has** `{handSize}|{deckSize}|{discardSize}|{exhaustSize}` hand|deck|discard|exhaust cards <\\");
        }
        
        _db.TotalTurnsPlayed += 1;
        return Task.CompletedTask;       
    }

    public override Task AfterOstyRevived(Creature osty)
    {
        if (IsMeOrMine(osty))
        {
            WriteIt($"> My {FormatCreature(osty)} **revived** <\\");
            _db.TotalOstyRevives += 1;
        }
        else
        {
            WriteIt($"> Another {FormatCreature(osty)} **revived** <\\");
        }
        return Task.CompletedTask;
    }

    public override Task AfterEnergyReset(Player player)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        var currentEnergy = player.PlayerCombatState?.Energy ?? player.MaxEnergy;
        WriteIt($"> {FormatPlayer(player)} **reset** `{currentEnergy}/{player.MaxEnergy}` energy <\\");
        return Task.CompletedTask;
    }

    public void OnEnergyGained(Decimal amount)
    {
        WriteIt($"> I **gained** `{(int) amount}` energy <\\");
        _db.TotalEnergyGained += (int) amount;
    }
    
    public override Task AfterStarsGained(int amount, Player gainer)
    {
        if (!LocalContext.IsMe(gainer)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(gainer)} **gained** `{amount}` stars <\\");
        _db.TotalStarsGained += amount;
        return Task.CompletedTask;
    }

    public override Task AfterCardDrawn(PlayerChoiceContext ctx, CardModel card, bool fromHandDraw)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        
        var keywords = string.Join(", ", card.Keywords.Select(keyword => $"`{keyword}`"));
        var tags = string.Join(", ", card.Tags.Select(tag => $"`{tag}`"));

        var dynamicVars = string.Join(", ", card.DynamicVars.Values.Select(dynamicVar => $"`{dynamicVar.Name.Replace("Power", "")} {(int) dynamicVar.EnchantedValue}`"));
        
        var enchantment= card.Enchantment?.Title.ToString() ?? "";
        var affliction = card.Affliction?.Title.ToString() ?? "";

        var replayCount = card.GetEnchantedReplayCount();
        var replayEntry = (replayCount > 0) ? $"[Replay: `{replayCount.ToString()}`]" : "";
        
        var energyCost = (card.EnergyCost.CostsX) ? "X" : card.EnergyCost.Canonical.ToString();
        var starCost = (card.HasStarCostX) ? "X" : card.CurrentStarCost.ToString();

        if (card.CurrentStarCost > 0 || card.HasStarCostX)
        {
            WriteIt($"> I **drew** {FormatCard(card)} [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy **and** `{starCost}` stars <\\");
        }
        else
        {
            WriteIt($"> I **drew** {FormatCard(card)} [{dynamicVars}] [{tags}] [{keywords}] [{enchantment}] [{affliction}] {replayEntry} **costing** `{energyCost}` energy <\\");
        }

        _db.TotalCardsDrawn += 1;
        return Task.CompletedTask;
    }

    public override Task AfterEnergySpent(CardModel card, int amount)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        WriteIt($"> {FormatCard(card)} **cost** `{amount}` energy <\\");
        _db.TotalEnergySpent += amount;
        return Task.CompletedTask;
    }

    public override Task AfterStarsSpent(int amount, Player spender)
    {
        if (!LocalContext.IsMe(spender)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(spender)} **spent** `{amount}` stars <\\");
        _db.TotalStarsSpent += amount; 
        return Task.CompletedTask;
    }

    public override Task AfterPotionUsed(PotionModel potion, Creature? target)
    {
        var prefix = (LocalContext.IsMe(potion.Owner)) ? "I" : "Another";
        if (target != null)
        {
            WriteIt($"> _{prefix} **used** `{potion.Title.GetFormattedText()}` **on** {FormatCreature(target)}_ <\\");
        }
        else
        {
            WriteIt($"> _{prefix} **used** `{potion.Title.GetFormattedText()}`_ <\\");
        }

        if (LocalContext.IsMe(potion.Owner))
        {
            _db.TotalPotionsUsed += 1;
        }
        return Task.CompletedTask;
    }

    public override Task AfterPotionDiscarded(PotionModel potion)
    {
        if (!LocalContext.IsMe(potion.Owner)) return Task.CompletedTask;
        WriteIt($"> _I **discarded** `{potion.Title}`_ <\\");
        _db.TotalPotionsDiscarded += 1;
        return Task.CompletedTask;
    }

    public override Task AfterForge(Decimal amount, Player forger, AbstractModel? source)
    {
        if (!LocalContext.IsMe(forger)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(forger)} **forged** `{(int) amount}` <\\");
        _db.TotalForged += (int) amount;
        return Task.CompletedTask;
    }

    public override Task AfterSummon(PlayerChoiceContext ctx, Player summoner, Decimal amount)
    {
        if (!LocalContext.IsMe(summoner)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(summoner)} **summoned** `{(int) amount}` <\\");
        _db.TotalSummoned += (int) amount;
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

        if (LocalContext.IsMe(card.Owner))
        {
            _db.TotalCardsPlayed += 1;
            _db.AddCardPlay(card.Title);
        }
    }

    public void RecordPower(PowerModel power, Creature target, Decimal amount, Creature? applier,
        CardModel? cardSource)
    {
        if (amount <= 0) return;
        
        var isMyCard = cardSource != null && LocalContext.IsMe(cardSource.Owner);
        
        if (power.Title.GetFormattedText().Contains("Strength") && (IsMeOrMine(target) || isMyCard))
        {
            _db.TotalStrengthGained += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Vulnerable") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            _db.TotalVulnerableApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Weak") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            _db.TotalWeakApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Poison") && target.IsEnemy && (IsMeOrMine(applier) || isMyCard))
        {
            _db.TotalPoisonApplied += (int) amount;
        }
        else if (power.Title.GetFormattedText().Contains("Doom") && (IsMeOrMine(applier) || isMyCard))
        {
            _db.TotalDoomApplied += (int) amount;
        }
    }

    public override Task AfterOrbChanneled(PlayerChoiceContext choiceContext, Player player, OrbModel orb)
    {
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        WriteIt($"> {FormatPlayer(player)} **channeled** `{orb.Title}` <\\");
        _db.TotalOrbsChanneled += 1;
        return Task.CompletedTask;
    }

    public override Task AfterOrbEvoked(PlayerChoiceContext choiceContext, OrbModel orb, IEnumerable<Creature> targets)
    {
        if (!LocalContext.IsMe(orb.Owner)) return Task.CompletedTask;
        WriteIt($"> `{orb.Title}` **evoked** <\\");
        _db.TotalOrbsEvoked += 1;
        return Task.CompletedTask;
    }
    
    public void RecordEnemyIntent(Creature owner, MoveState state)
    {
        if (!_db.InCombat) return;
        var intentions = string.Join(
            ", ",
            state.Intents.Select(intention => {
                if (intention is AttackIntent attackIntention)
                {
                    var dmg = attackIntention.DamageCalc?.Invoke() ?? -1;
                    if (attackIntention.Repeats > 1)
                    {
                        return $"`{intention.IntentType.ToString()} {(int) dmg}x{attackIntention.Repeats}`";
                    }
                    else
                    {
                        return $"`{intention.IntentType.ToString()} {(int) dmg}`";
                    }
                }
                return $"`{intention.IntentType.ToString()}`";
            }));
        WriteIt($"> {FormatCreature(owner)} **intends** `{state.Id}` [{intentions}] <\\");
    }
    
    // if applying damages results in combat ending, then combat ends without AfterDamageReceived firing
    // so this is used to ensure that all possible damage events are observed
    public override Task BeforeDamageReceived(PlayerChoiceContext ctx, Creature target, Decimal amount,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        var permittedBlock = (dealer != null && dealer.Name == target.Name) ? 0 : target.Block;
        if (permittedBlock + target.CurrentHp > (int) amount)
        {
            return Task.CompletedTask;
        }
        
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **using** `{cardSource.Title}` [`Damage {(int) amount}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} [`Damage {(int) amount}`] **hitting** {FormatCreature(target)} <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {(int) amount}`] **targeting** {FormatCreature(target)} <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **receiving** [`Damage {(int) amount}`] <\\");
        }

        RecordDamageTotals(target, dealer, cardSource, (int) amount, target.CurrentHp, target.Block);
        return Task.CompletedTask;
    }
    
    public override Task AfterDamageReceived(PlayerChoiceContext ctx, Creature target, DamageResult result,
        ValueProp props, Creature? dealer, CardModel? cardSource)
    {
        if (dealer != null && cardSource != null)
        {
            WriteIt($"> {FormatCreature(dealer)} **used** `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **against** {FormatCreature(target)} <\\");
        }
        else if (dealer != null)
        {
            WriteIt($"> {FormatCreature(dealer)} [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **hit** {FormatCreature(target)} <\\");
        }
        else if (cardSource != null)
        {
            WriteIt($"> `{cardSource.Title}` [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] **targeted** {FormatCreature(target)} <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(target)} **received** [`Damage {result.BlockedDamage}|{result.UnblockedDamage}`] <\\");
        }
        
        RecordDamageTotals(target, dealer, cardSource, result.TotalDamage, result.UnblockedDamage, result.BlockedDamage);
        return Task.CompletedTask;
    }

    public void OnCreatureHeal(Creature creature, Decimal amount)
    {
        WriteIt($"> {FormatCreature(creature)} **healed** `{(int) amount}` HP <\\");
        if (!LocalContext.IsMe(creature)) return;
        _db.TotalHpHealed += (int) amount;
    }

    private void RecordDamageTotals(Creature target, Creature? dealer, CardModel? cardSource, int totalDamage, int? trueDamage, int? blockedDamage)
    {
        if (target.IsEnemy && IsMeOrMine(dealer))
        {
            _db.AddCombatDamageDealt(totalDamage, trueDamage, blockedDamage);
            if (dealer is { IsPet: true })
            {
                _db.TotalPetDamage += totalDamage;
            }
        }
        else if (IsMeOrMine(target))
        {
            if (target is { IsPet: true })
            {
                _db.TotalPetDamageReceived += Math.Min(totalDamage, target.Block + target.CurrentHp);
            }
            else
            {
                _db.AddCombatDamageReceived(totalDamage, trueDamage, blockedDamage);
                if (dealer != null && LocalContext.IsMe(dealer.Player))
                {
                    _db.TotalSelfDamage += totalDamage;
                }
            }
        }       
        if (cardSource != null && LocalContext.IsMe(cardSource.Owner))
        {
            _db.AddDamageDealt(cardSource.Title, totalDamage);
        }
        else if (cardSource == null && dealer != null && LocalContext.IsMe(dealer.Player))
        {
            _db.TotalAnonymousDamage += totalDamage;
        }
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
            WriteIt($"> {FormatCreature(creature)} **used** `{cardSource.Title}` [`Block {amount}`] <\\");
        }
        else
        {
            WriteIt($"> {FormatCreature(creature)} **gained** [`Block {amount}`] <\\");
        }

        if (!LocalContext.IsMe(creature.Player)) return Task.CompletedTask;
        
        _db.AddCombatBlockGained((int) amount);
        if (cardSource != null)
        {
            _db.AddBlockGained(cardSource.Title, (int) amount);
        }
        else
        {
            _db.TotalAnonymousBlock += (int) amount;
        }
        return Task.CompletedTask;
    }

    public override Task AfterCardRetained(CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _db.TotalCardsRetained += 1;
        WriteIt($"> I **retained** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCardDiscarded(PlayerChoiceContext ctx, CardModel card)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _db.TotalCardsDiscarded += 1;
        WriteIt($"> I **discarded** {FormatCard(card)} <\\");
        return Task.CompletedTask;
    }

    public override Task AfterCardExhausted(PlayerChoiceContext ctx, CardModel card, bool causedByEthereal)
    {
        if (!LocalContext.IsMe(card.Owner)) return Task.CompletedTask;
        _db.TotalCardsExhausted += 1;
        WriteIt($"> I **exhausted** {FormatCard(card)} <\\");
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
        if (!LocalContext.IsMe(player)) return Task.CompletedTask;
        _db.TotalEmptyHands += 1;
        WriteIt($"> {FormatPlayer(player)} **emptied** `hand` <\\");
        return Task.CompletedTask;
    }

    public override Task AfterShuffle(PlayerChoiceContext ctx, Player shuffler)
    {
        if (!LocalContext.IsMe(shuffler)) return Task.CompletedTask;
        _db.TotalDeckShuffles += 1;
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
            if (_savePath != null && File.Exists(_savePath) && finalPath != null)
            {
                File.Move(_savePath, finalPath, overwrite: true);
                MainFile.Logger.Info($"Combat history saved to: {finalPath}");
                
                _savePath = null;
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
        return Path.Combine(destDir, $"sts2_combat_tracker_{startTime}.replay");
    }

    private static bool IsMeOrMine(Creature? creature)
    {
        return creature != null && (LocalContext.IsMe(creature) || (creature is { IsPet: true } && LocalContext.IsMe(creature.PetOwner)));
    }
    
    private void WriteIt(string msg)
    {
        if (_writerTask != null)
        {
            _queue.Enqueue(msg);
        }
    }
}