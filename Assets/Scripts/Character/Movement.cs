using UnityEngine;
using UnityEngine.InputSystem;

public class Movement : MonoBehaviour
{
    private PlayerInput _input;
    private Camera _camera;

    private const float MinDirectionSqrMagnitude = 0.0001f;

    [Header("Steering")]
    [SerializeField] private float _rotationSpeed = 720f;
    [SerializeField] private bool _faceCameraForwardWhenIdle = false;
    [SerializeField] private bool debugSteering = false;

    private void Start()
    {
        _input = GetComponent<PlayerInput>();
        _camera = Camera.main;
    }

    private void Update()
    {
        Vector3 moveDirection = CalculateCameraRelativeMoveDirection();
        RotatePlayer(GetFacingDirection(moveDirection));
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
}