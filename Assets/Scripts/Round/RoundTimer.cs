using System;
using NUnit.Framework;


public class RoundTimer
{
    public float Timer = 5f;

    public float StartRoundTimer = 20f;

    public float CurrentTime { get; private set; }
    public bool IsRunning { get; private set; }

    public event Action OnTimeout;
    public event Action<float> OnTick;

    public RoundTimer(float startTime)
    {
        CurrentTime = startTime;
    }

    public void Start()
    {
        if (!IsRunning)
            IsRunning = true;
    }

    public void RefreshTime()
    {
        CurrentTime = StartRoundTimer;
    }

    public void Stop()
    {
        IsRunning = false;
    }

    public void Tick(float delta)
    {
        if (!IsRunning) return;

        CurrentTime -= delta;
        OnTick?.Invoke(CurrentTime);

        if (CurrentTime <= 0f)
        {
            IsRunning = false;
            RefreshTime();
            OnTimeout?.Invoke();
        }
    }
}
