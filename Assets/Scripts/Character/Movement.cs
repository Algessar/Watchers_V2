using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.Playables;

public class Movement : MonoBehaviour
{
    private AnimationSystem_v2 _animSystem;
    private PlayerInput _input;
    private Camera _camera;
    private Combat _combat;

    private const float MinDirectionSqrMagnitude = 0.0001f;

    [Header("Steering")]
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private bool _faceCameraForwardWhenIdle = false;
    [SerializeField] private bool debugSteering = false;

    private void Start()
    {
        _cam = Camera.main;
        _input = GetComponent<PlayerInput>();
        _camera = Camera.main;
        _combat = GetComponent<Combat>();
        _animSystem = GetComponent<AnimationSystem_v2>();
    }

    private void Update()
    {
        Vector3 moveDirection = CalculateCameraRelativeMoveDirection();
        
        if (_combat == null || !_combat.IsLockedOn)
        {
            RotatePlayer(GetFacingDirection(moveDirection));
        }
        // RotatePlayer(GetFacingDirection(moveDirection));
    }

    private Vector3 CalculateCameraRelativeMoveDirection()
    {
        Vector2 input = Vector2.ClampMagnitude(_input.direction, 1f);
        if (input.sqrMagnitude <= MinDirectionSqrMagnitude)
            return Vector3.zero;

        Vector3 forward = GetPlanarCameraForward();
        Vector3 right = GetPlanarCameraRight();

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);
    }

    private Vector3 GetFacingDirection(Vector3 currentMoveDirection)
    {
        if (currentMoveDirection.sqrMagnitude > MinDirectionSqrMagnitude)
            return currentMoveDirection;

        return _faceCameraForwardWhenIdle ? GetPlanarCameraForward() : Vector3.zero;
    }

    private Vector3 GetPlanarCameraForward()
    {
        Transform referenceTransform = _camera != null ? _camera.transform : transform;
        Vector3 forward = Vector3.ProjectOnPlane(referenceTransform.forward, Vector3.up);
        return forward.sqrMagnitude > MinDirectionSqrMagnitude ? forward.normalized : transform.forward;
    }

    private Vector3 GetPlanarCameraRight()
    {
        Transform referenceTransform = _camera != null ? _camera.transform : transform;
        Vector3 right = Vector3.ProjectOnPlane(referenceTransform.right, Vector3.up);
        return right.sqrMagnitude > MinDirectionSqrMagnitude ? right.normalized : transform.right;
    }

    private Vector3 RotatePlayer(Vector3 targetDirection)
    {
        targetDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up);
        if (targetDirection.sqrMagnitude <= MinDirectionSqrMagnitude)
            return transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime);

        if (debugSteering)
        {
            Debug.Log(
                $"[Movement] Steering target={targetDirection.normalized} rotation={transform.eulerAngles} input={_input.direction}",
                this);
        }
        
        return targetDirection.normalized;
    }
    
    private float _currentForwardSpeed;
    private float _currentStrafeFactor;
    private Camera _cam;

    public void UpdateLocomotionBlends()
    {
        float speed = _animSystem.GetMovementSpeed();
        bool isLocked = _combat != null && _combat.IsLockedOn;

        if (!isLocked)
        {
            // No target: classic forward-only locomotion
            _animSystem.UpdateLocomotionWeights(speed);
            return;
        }

        // Locked on: compute forward/strafe from input relative to character's facing (toward target)
        Vector3 inputDir = GetCameraRelativeMoveDirection(); // reuse from Movement
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        float forwardInput = Vector3.Dot(inputDir, forward);
        float rightInput = Vector3.Dot(inputDir, right);

        _currentForwardSpeed = Mathf.Clamp01(speed) * Mathf.Clamp01(forwardInput); // forward only (0..1)
        float strafe = rightInput; // -1 left, +1 right

        // Map forward speed to walk/run as before (using _runThreshold)
        float clampedSpeed = Mathf.Clamp01(_currentForwardSpeed);
        float walkWeight = 0f, runWeight = 0f;

        if (clampedSpeed <= _animSystem.RunThreshold)
        {
            float t = _animSystem.RunThreshold > 0f ? clampedSpeed / _animSystem.RunThreshold : 0f;
            walkWeight = t;
            // idle weight handled separately, but we keep idle at 1-t only if no strafe?
            // Simpler: let idle weight = 0 when moving, 1 when stopped.
        }
        else
        {
            float t = _animSystem.RunThreshold < 1f ? (clampedSpeed - _animSystem.RunThreshold) / (1f - _animSystem.RunThreshold) : 1f;
            walkWeight = 1 - t;
            runWeight = t;
        }

        float idleWeight = (clampedSpeed < 0.05f && Mathf.Abs(strafe) < 0.05f) ? 1f : 0f;

        // Blend strafe weights
        float leftWeight = Mathf.Max(0f, -strafe);
        float rightWeight = Mathf.Max(0f, strafe);

        // Apply to mixer
        _animSystem._locomotionMixer.SetInputWeight(_animSystem._idlePort, idleWeight);
        _animSystem._locomotionMixer.SetInputWeight(_animSystem._walkPort, walkWeight);
        _animSystem._locomotionMixer.SetInputWeight(_animSystem._runPort, runWeight);
        _animSystem._locomotionMixer.SetInputWeight(_animSystem._strafePortLeft, leftWeight);
        _animSystem._locomotionMixer.SetInputWeight(_animSystem._strafePortRight, rightWeight);
    }

    // Helper to get camera-relative input direction (same as Movement.CalculateCameraRelativeMoveDirection)
    private Vector3 GetCameraRelativeMoveDirection()
    {
        if (!_input) return Vector3.zero;
        Vector2 input = Vector2.ClampMagnitude(_input.direction, 1f);
        if (input.sqrMagnitude < 0.0001f) return Vector3.zero;

        if (!_cam) return new Vector3(input.x, 0f, input.y).normalized;

        Vector3 forward = Vector3.ProjectOnPlane(_cam.transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(_cam.transform.right, Vector3.up).normalized;
        return (forward * input.y + right * input.x).normalized;
    }
}