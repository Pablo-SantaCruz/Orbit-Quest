using UnityEngine;

public class OrbitPathLine
{
    readonly LineRenderer _renderer;

    public OrbitPathLine(LineRenderer renderer)
    {
        _renderer = renderer;
        _renderer.loop = true;
        _renderer.useWorldSpace = true;
        _renderer.enabled = false;
    }

    public void Update(bool visible, Vector3[] points, int count)
    {
        if (!visible || count < 3)
        {
            _renderer.enabled = false;
            return;
        }

        _renderer.enabled = true;
        _renderer.positionCount = count;
        for (int i = 0; i < count; i++)
            _renderer.SetPosition(i, points[i]);
    }
}
