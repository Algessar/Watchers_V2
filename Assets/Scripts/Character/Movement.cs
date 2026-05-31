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
    private Camera _camera;
    private Vector3 _targetDirection;

    private AnimationSystem_v1 _animSystemV1;
    private int _idleId;
    private int _walkId;
    private int _runId;
    
    public AnimationClip idle;
    public AnimationClip walk;
    public AnimationClip run;

    //Input
    private PlayerInput _input;
    private Gamepad _gamepad;

    
    // Tweak these thresholds to your liking
    [SerializeField] private float walkStartSpeed = 0.2f;
    [SerializeField] private float runStartSpeed = 0.7f;
    
    public Vector3 moveDirection;
    public float moveSpeed = 0.5f;
    private float _baseRotationSpeed;

    private void Start()
    {
        _gamepad = Gamepad.current ?? Gamepad.all.FirstOrDefault();
        // _gamepad = Gamepad.current;
        _controller = GetComponent<CharacterController>();
        _input = GetComponent<PlayerInput>();
        // _animSystemV1 = GetComponent<AnimationSystem_v1>();

        _camera = Camera.main;

        if (_gamepad == null)
        {
            Debug.Log("Gamepad is null");
        }

        // _idleId = _animSystemV1.RegisterClip(idle, 0);
        // _walkId = _animSystemV1.RegisterClip(walk, 0);
        // _runId = _animSystemV1.RegisterClip(run, 0);
        //
        // Debug.Log($"IdleId: {_idleId}, WalkId: {_walkId}, RunId: {_runId}");
        //
        // _animSystemV1.SetClipWeight(_idleId, 1f);

    }

    public void Update()
    {
        moveDirection = CalculateCamRelativeDir();

        Controller(moveDirection * (moveSpeed * Time.deltaTime));
        RotatePlayer(moveDirection);
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


    Vector3 CalculateCamRelativeDir()
    {
        var inputDirection = NormalizedMoveVector();

        Transform camTransform = _camera.transform;

        Vector3 camForward = camTransform.forward;
        Vector3 camRight = camTransform.right;

        Vector3 moveDir = Vector3.zero;

        moveDir = camForward * inputDirection.z + camRight * inputDirection.x;
        moveDir.Normalize();
        moveDir.y = 0;
        return moveDir;
    }
    
    public Vector3 RotatePlayer(Vector3 targetDirection)
    {
        if (_targetDirection.sqrMagnitude > 0.01f)
        {
            Quaternion targetRotation = Quaternion.LookRotation(_targetDirection, Vector3.up);
            transform.rotation = Quaternion.Slerp(
                transform.rotation, targetRotation, 
                _baseRotationSpeed * Time.deltaTime);
        }

        return _targetDirection;
    }

}