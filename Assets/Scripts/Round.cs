using System;

public class Round
{
    public int RoundCount { get; set; } = 0;
    private int remainingSpawnCount;

    public RoundTimer Timer { get; }
    public RoundMonsterCounter MonsterCounter { get; }

    public int RoundInSpawnedMonsterCount => MonsterCounter.RoundInSpawnedMonsterCount;

    public event Action<int> OnRoundEnded;

    public Round()
    {
        Timer = new RoundTimer(5f);
        MonsterCounter = new RoundMonsterCounter(100);

        Timer.OnTimeout += EndRound;
        MonsterCounter.OnAllDefeated += EndRound;
        remainingSpawnCount = RoundInSpawnedMonsterCount;
    }
    public bool CanSpawn => remainingSpawnCount > 0;



    public void OnMonsterSpawned()
    {
        remainingSpawnCount--;
        MonsterCounter.OnMonsterSpawned();
    }

    public void OnMonsterDefeated()
    {
        MonsterCounter.OnMonsterDefeated();
    }

    private void EndRound()
    {
        remainingSpawnCount = RoundInSpawnedMonsterCount;
        RoundCount++;
        OnRoundEnded?.Invoke(RoundCount);
    }
    public void Tick(float delta)
    {
        Timer.Tick(delta);
    }


}
