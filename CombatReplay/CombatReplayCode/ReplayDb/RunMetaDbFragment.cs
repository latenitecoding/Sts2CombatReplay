using System.Text.Json;
using System.Text.RegularExpressions;
using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    public string? RunSeed { get; set; }
    public bool IsMultiplayer { get; init; }
    public int CurrentAct { get; set; }
    public int CurrentRoom { get; set; }
    public int CurrentCombat { get; set; }
    
    public int TotalPotionsDiscarded { get; set; }
    public int TotalHpHealed { get; set; }
    
    public void NextAct() => CurrentAct += 1;
    public void NextRoom() => CurrentRoom += 1;
}