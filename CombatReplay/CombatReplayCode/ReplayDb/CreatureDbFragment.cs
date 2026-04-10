using MegaCrit.Sts2.Core.Entities.Creatures;

namespace CombatReplay.CombatReplayCode.ReplayDb;

public partial class CombatReplayDb
{
    private readonly List<Creature> _currentCreatures = [];
    
    public int TotalEnemiesFought { get; set; }
    public IReadOnlyList<Creature> GetCombatCreatureList() => _currentCreatures;
    
    public void OnAddCreature(Creature creature, string fmtTitle)
    {
        _currentCreatures.Add(creature);
        if (!creature.IsEnemy) return;
        _currentCombat.Enemies.Add(fmtTitle);
        TotalEnemiesFought += 1;
    }
}