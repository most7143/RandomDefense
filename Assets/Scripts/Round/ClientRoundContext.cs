using System;
using System.Diagnostics;
public class ClientRoundContext
{
    public int RemainingSpawnCount { get; set; } = 40;
    public int AliveMonsterCount;

    public bool CanSpawn => RemainingSpawnCount > 0;
    public bool IsCleared => RemainingSpawnCount == 0 && AliveMonsterCount == 0;

    public event Action OnLocalRoundCleared;

    public Action<int> OnAliveMonsterCountChanged;


    public void OnMonsterSpawned()
    {
        AliveMonsterCount++;
        OnAliveMonsterCountChanged.Invoke(AliveMonsterCount);
    }

    public void OnMonsterDead()
    {
        AliveMonsterCount--;

        if (IsCleared)
            OnLocalRoundCleared?.Invoke();

        OnAliveMonsterCountChanged.Invoke(AliveMonsterCount);
    }

    public void ResetForNewRound(int round)
    {
        RemainingSpawnCount = 40;
        OnAliveMonsterCountChanged?.Invoke(AliveMonsterCount);
    }

    public bool TryConsumeSpawn()
    {

        if (RemainingSpawnCount <= 0)
            return false;

        RemainingSpawnCount--;

        return true;
    }

}
