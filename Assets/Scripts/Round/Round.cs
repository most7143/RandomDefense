using System;

public class Round
{
    public int RoundCount { get; set; } = 0;

    public int MaxSpawnCount = 100;

    public float StartTimer = 5f;

    public int RoundIndex { get; private set; }

    public RoundTimer Timer { get; }

    public event Action<int> OnRoundEnded;

    public event Action OnRoundFailed;

    public Round()
    {
        Timer = new RoundTimer(StartTimer);
        Timer.OnTimeout += EndRound;
    }

    public void Start()
    {
        Timer.Start();
    }

    private void EndRound()
    {
        RoundIndex++;
        OnRoundEnded?.Invoke(RoundIndex);
    }

    public void FailRound()
    {
        OnRoundFailed?.Invoke();
    }

    public void Tick(float delta)
    {
        Timer.Tick(delta);
    }


}
