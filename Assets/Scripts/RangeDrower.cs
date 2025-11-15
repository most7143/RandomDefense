using UnityEngine;


public class RangeDrawer : MonoBehaviour
{
    public float radius = 3f;
    public int segments = 64;
    public LineRenderer line;

    void Start()
    {
        line.positionCount = segments + 1;
        float angle = 0f;

        for (int i = 0; i <= segments; i++)
        {
            float x = Mathf.Cos(angle) * radius;
            float y = Mathf.Sin(angle) * radius;

            line.SetPosition(i, new Vector3(x, y, 0));
            angle += 2 * Mathf.PI / segments;
        }
    }
}