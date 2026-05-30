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

        // float speed = _input.moveAmount; // 0...1
        //
        // // Compute continuous weights
        // float idleWeight = 0f;
        // float walkWeight = 0f;
        // float runWeight  = 0f;
        //
        // if (speed < walkStartSpeed)
        // {
        //     // Only idle, full weight = 1, others 0
        //     idleWeight = 1f;
        // }
        // else if (speed < runStartSpeed)
        // {
        //     // Blend between idle and walk
        //     float t = Mathf.InverseLerp(walkStartSpeed, runStartSpeed, speed);
        //     // But we want idle to fade out and walk to fade in
        //     idleWeight = 1f - t;
        //     walkWeight = t;
        // }
        // else
        // {
        //     // Blend between walk and run
        //     float t = Mathf.InverseLerp(runStartSpeed, 1f, speed);
        //     walkWeight = 1f - t;
        //     runWeight  = t;
        // }
        //
        // // Apply weights directly (no timer, no crossfade)
        // _animSystemV1.SetClipWeight(_idleId, idleWeight);
        // _animSystemV1.SetClipWeight(_walkId, walkWeight);
        // _animSystemV1.SetClipWeight(_runId,  runWeight);
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

}