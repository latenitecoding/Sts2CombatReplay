using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Runs;

namespace CombatReplay.CombatReplayCode;
using Patches;
using Tracker;

[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "CombatReplay"; //At the moment, this is used only for the Logger and harmony names.
    public const string GameVersion = "[v0.106.1] (2026.05.23)";
    public const string ModVersion = "[v1.7.3] (2026.05.28)";

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static readonly CombatReplayTracker Tracker = new();
    
    private static void OnRunStarted(RunState state) => Tracker.OnRunStart(state);
    private static void OnActEntered() => Tracker.OnActEntered();
    private static void OnRoomExited() => Tracker.OnRoomExited();

    public static void Initialize()
    {
        Logger.Info("Initializing CombatReplay...");
        
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        Logger.Info("Harmony patched successfully.");
        
        // the best (if not only) start of run hook to grab
        RunManager.Instance.RunStarted += OnRunStarted;
        // the best (if not only) act start hook
        RunManager.Instance.ActEntered += OnActEntered;
        // the best (if not only) room exited hook
        RunManager.Instance.RoomExited += OnRoomExited;
        // there is also RunManager.Instance.RoomEntered which is not being used
        // because there are many events that are triggered before RoomEntered
        // but always after RoomExited, so OnRoomExited calls OnRoomEntered
        // to ensure that those events are grouped into the next room
        // start of combat events are one such example

        Logger.Info("RunManager event handlers set");
        
        ModHelper.SubscribeForCombatStateHooks(ModId, combatState => new List<AbstractModel> { Tracker });
        
        Logger.Info("ModHelper hooked");
    }
}