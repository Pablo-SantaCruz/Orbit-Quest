using UnityEngine;

public class LineRendererController
{
    readonly LineRenderer _trajectoryRenderer;
    readonly LineRenderer _directionRenderer;
    readonly LineRenderer _tensionRenderer;
    readonly RigidbodyOrbiter _orbiter;
    readonly float _directionLength;
    readonly MaterialPropertyBlock _tensionPropertyBlock;
    readonly int _stabilityId = Shader.PropertyToID("_Stability");
    private readonly Vector3[] _directionPoints = new Vector3[2];
    private readonly Vector3[] _tensionPoints = new Vector3[2];
    private bool _isAiming;

    public LineRendererController(LineRenderer trajectoryRenderer, LineRenderer directionRenderer, LineRenderer tensionRenderer, RigidbodyOrbiter orbiter, float directionLength)
    {
        _trajectoryRenderer = trajectoryRenderer;
        _trajectoryRenderer.enabled = false;
        _trajectoryRenderer.textureMode = LineTextureMode.Stretch;

        _directionRenderer = directionRenderer;
        _directionRenderer.enabled = false;
        _directionRenderer.positionCount = 2;

        _tensionRenderer = tensionRenderer;
        _tensionRenderer.enabled = false;
        _tensionRenderer.positionCount = 2;
        _tensionRenderer.textureMode = LineTextureMode.Stretch;
        _tensionPropertyBlock = new MaterialPropertyBlock();

        _orbiter = orbiter;
        _directionLength = directionLength;
    }

    public void SetAiming(bool active)
    {
        _isAiming = active;
        _trajectoryRenderer.enabled = active;
    }

    public void UpdateTrajectory(Vector3 cursorWorldPosition)
    {
        if (!_isAiming)
            return;

        int count = _orbiter.PredictTrajectory(cursorWorldPosition);
        _trajectoryRenderer.positionCount = count;
        _trajectoryRenderer.SetPositions(_orbiter.TrajectoryPoints);
    }

    public void UpdateDirection(Vector3 position, Vector3 velocity)
    {
        if (velocity.sqrMagnitude < 0.001f)
        {
            _directionRenderer.enabled = false;
            return;
        }

        _directionRenderer.enabled = true;

        _directionPoints[0] = position;
        _directionPoints[1] = position + velocity.normalized * _directionLength;

        _directionRenderer.SetPositions(_directionPoints);
    }

    public void UpdateTension(bool visible, Vector3 orbPosition, Vector3 astroPosition, float stability)
    {
        if (!visible)
        {
            _tensionRenderer.enabled = false;
            return;
        }

        _tensionRenderer.enabled = true;
        _tensionPoints[0] = orbPosition;
        _tensionPoints[1] = astroPosition;
        _tensionRenderer.SetPositions(_tensionPoints);

        _tensionRenderer.GetPropertyBlock(_tensionPropertyBlock);
        _tensionPropertyBlock.SetFloat(_stabilityId, Mathf.Clamp01(stability));
        _tensionRenderer.SetPropertyBlock(_tensionPropertyBlock);
    }
}
