using Godot;
using HarmonyLib;
using MegaCrit.Sts2.Core.Combat;
using MegaCrit.Sts2.Core.GameActions;
using MegaCrit.Sts2.Core.Modding;
using MegaCrit.Sts2.Core.Models;
using MegaCrit.Sts2.Core.Rooms;
using MegaCrit.Sts2.Core.Runs;

namespace CombatReplay.CombatReplayCode;

//You're recommended but not required to keep all your code in this package and all your assets in the RunReplay folder.
[ModInitializer(nameof(Initialize))]
public partial class MainFile : Node
{
    private const string ModId = "CombatReplay"; //At the moment, this is used only for the Logger and harmony names.

    public static MegaCrit.Sts2.Core.Logging.Logger Logger { get; } =
        new(ModId, MegaCrit.Sts2.Core.Logging.LogType.Generic);

    public static readonly CombatActionTracker Tracker = new();
    
    private static void OnRunStarted(RunState state) => Tracker.OnRunStarted();
    private static void OnRoomExited() => Tracker.OnRoomExited();
    
    public static void Initialize()
    {
        Logger.Info("Initializing CombatReplay...");
        
        Harmony harmony = new(ModId);
        harmony.PatchAll();

        Logger.Info("Harmony patched successfully.");
        
        RunManager.Instance.RunStarted += OnRunStarted;
        RunManager.Instance.RoomExited += OnRoomExited;

        Logger.Info("RunManager event handlers set");
        
        ModHelper.SubscribeForCombatStateHooks(ModId, combatState => new List<AbstractModel> { Tracker });
        
        Logger.Info("ModHelper hooked");
    }
}