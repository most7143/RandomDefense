using System;

public class RoundMonsterCounter
{
    public int CurrentCount { get; private set; }
    public int MaxCount { get; }

    public int RoundInSpawnedMonsterCount { get; set; } = 20;

    public event Action<int> OnCountChanged;
    public event Action OnAllDefeated;

    public RoundMonsterCounter(int maxCount)
    {
        MaxCount = maxCount;
    }

    public void OnMonsterSpawned()
    {
        CurrentCount++;
        OnCountChanged?.Invoke(CurrentCount);
    }

    public void OnMonsterDefeated()
    {
        CurrentCount--;
        OnCountChanged?.Invoke(CurrentCount);

        if (CurrentCount <= 0)
            OnAllDefeated?.Invoke();
    }
}
