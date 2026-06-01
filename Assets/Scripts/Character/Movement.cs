using System;
using System.Linq;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.LowLevel;
using UnityEngine.Playables;


public class Movement : MonoBehaviour
{
    private CharacterController _controller;
    private AnimationSystem_v2 _animSystem;
    private Camera _camera;

    //Input
    private PlayerInput _input;
    private Gamepad _gamepad;


    // Tweak these thresholds to your liking
    private const float MinDirectionSqrMagnitude = 0.0001f;

    [SerializeField] private float walkStartSpeed = 0.2f;
    [SerializeField] private float runStartSpeed = 0.7f;

    public Vector3 moveDirection;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _currentSpeed = 0.5f;
    [SerializeField] private float _halfSpeed = 0f;

    [Header("Steering")] [SerializeField] private float _rotationSpeed = 720f;

    [SerializeField] private bool _faceCameraForwardWhenIdle = false;
    [SerializeField] private bool debugSteering = false;



    private void Start()
    {
        _gamepad = Gamepad.current ?? Gamepad.all.FirstOrDefault();
        // _gamepad = Gamepad.current;
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        _animSystem = GetComponent<AnimationSystem_v2>();

        _camera = Camera.main;

        if (_gamepad == null)
        {
            Debug.Log("Gamepad is null");
        }

        _halfSpeed = _moveSpeed / 2;

    }

    public void Update()
    {
        if (_animSystem.CurrentStancePort != -1)
        {
            _currentSpeed = _halfSpeed;
        }
        else
        {
            _currentSpeed = _moveSpeed;
        }
        
        moveDirection = CalculateCameraRelativeMoveDirection();

        Controller(moveDirection * (_currentSpeed * Time.deltaTime));
        RotatePlayer(GetFacingDirection(moveDirection));
    }


    void Controller(Vector3 dir)
    {
        _controller.Move(dir);
    }

    private Vector3 NormalizedMoveVector()
    {
        Vector3 input = new Vector3(_input.direction.x, 0, _input.direction.y);
        input.Normalize();
        return input;
    }


    Vector3 CalculateCameraRelativeMoveDirection()
    {

        Vector2 input = Vector2.ClampMagnitude(_input.direction, 1f);

        if (input.sqrMagnitude <= MinDirectionSqrMagnitude) return Vector3.zero;

        Vector3 forward = GetPlanarCameraForward();
        Vector3 right = GetPlanarCameraRight();

        Vector3 direction = forward * input.y + right * input.x;
        return Vector3.ClampMagnitude(direction, 1f);

    }

    private Vector3 GetFacingDirection(Vector3 currentMoveDirection)
    {
        if (currentMoveDirection.sqrMagnitude > MinDirectionSqrMagnitude)
        {
            return currentMoveDirection;
        }

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

    public Vector3 RotatePlayer(Vector3 targetDirection)
    {
        targetDirection = Vector3.ProjectOnPlane(targetDirection, Vector3.up);

        if (targetDirection.sqrMagnitude <= MinDirectionSqrMagnitude) return transform.forward;

        Quaternion targetRotation = Quaternion.LookRotation(targetDirection.normalized, Vector3.up);
        transform.rotation = Quaternion.RotateTowards(
            transform.rotation,
            targetRotation,
            _rotationSpeed * Time.deltaTime);

        if (debugSteering)
        {
            Debug.Log(
                $"[Movement] Steering target={targetDirection.normalized} rotation={transform.eulerAngles} moveDirection={moveDirection} input={_input.direction}",
                this);

        }
        return targetDirection.normalized;
    }
}