using System;

public class SummonProbability
{
    // 시작/끝 확률
    private const float StartNormal = 95f;
    private const float EndNormal = 50f;
    private const float StartRare = 4f;
    private const float EndRare = 35f;
    private const float StartEpic = 1f;
    private const float EndEpic = 15f;



    public static (float normal, float rare, float epic) GetProbabilities(int level)
    {
        float t = (level - 1) / 10f;      // 0~1
        float tSquared = t * t;           // 가속형 증가

        float normal = StartNormal + (EndNormal - StartNormal) * tSquared;
        float rare = StartRare + (EndRare - StartRare) * tSquared;
        float epic = StartEpic + (EndEpic - StartEpic) * tSquared;

        return (normal, rare, epic);
    }
}
