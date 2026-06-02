using System;
using UnityEngine;
using UnityEngine.InputSystem;

public class PlayerInput : MonoBehaviour
{
    private Actions _actions;

    public Vector2 direction;
    public Vector3 cameraDirection;
    public bool isMoving;

    private float vertical;
    private float horizontal;

    [SerializeField] public float moveAmount;

    [Header("Combat")] 
    public bool vomTagTrigger;

    public bool pflugTrigger;
    public bool alberTrigger;
    public bool ochsTrigger;
    public bool langortTrigger; // should be contextual / triggered during in-combat transition
    public bool ironGateTrigger;

    public bool leaveGuardTrigger;
    public bool targetLockTrigger;
    

    private void OnEnable()
    {
        _actions = new Actions();
        _actions.Player.Enable();

        _actions.Player.Move.performed   += OnMove;
        _actions.Player.Move.canceled    += OnMove;

        _actions.Player.Look.performed   += OnLook;
        _actions.Player.Look.canceled    += OnLook;
        
        //Combat

        _actions.Player.VomTag.started   += OnVomTag;
        
        _actions.Player.Pflug.started    += OnPflug;
        
        _actions.Player.Alber.started    += OnAlber;
        
        _actions.Player.Ochs.started     += OnOchs;
        
        _actions.Player.IronGate.started += OnIronGate;

        _actions.Player.LeaveGuard.started += OnLeaveGuard;

        _actions.Player.TargetLock.started += OnTargetLock;

    }



    public void OnMove(InputAction.CallbackContext ctx)
    {
        direction = ctx.ReadValue<Vector2>();
        isMoving = !(direction == Vector2.zero);

        MoveInput(direction.x, direction.y);

        // Debug.Log($"Moving in direction: {direction}. Move amount: {moveAmount}");
    }

    public void MoveInput(float x, float y)
    {
        vertical = y;
        horizontal = x;

        moveAmount = Mathf.Clamp01(Mathf.Abs(vertical) + Mathf.Abs(horizontal));
    }

    public void OnLook(InputAction.CallbackContext ctx)
    {
        cameraDirection = ctx.ReadValue<Vector2>();
        // Debug.Log($"Cam vector: {cameraDirection}");

    }
    
    void OnVomTag(InputAction.CallbackContext ctx)
    {
        vomTagTrigger = true;
    }

    void OnPflug(InputAction.CallbackContext ctx)
    {
            pflugTrigger = true;
    }
    
    void OnAlber(InputAction.CallbackContext ctx)
    {
            alberTrigger = true;
    }
    
    void OnOchs(InputAction.CallbackContext ctx)
    {
            ochsTrigger = true;
    }
    
    void OnIronGate(InputAction.CallbackContext ctx)
    {
            ironGateTrigger = true;
    }

    void OnLeaveGuard(InputAction.CallbackContext ctx)
    {
        leaveGuardTrigger = true;
    }
    
    private void OnTargetLock(InputAction.CallbackContext obj)
    {
        targetLockTrigger = true;
    }

    public bool ConsumeTargetLockTrigger()
    {
        if (!targetLockTrigger)
        {
            return false;
        }

        targetLockTrigger = false;
        return true;
    }


    private void OnDisable()
    {
        _actions.Player.Move.performed   -= OnMove;
        _actions.Player.Move.canceled    -= OnMove;

        _actions.Player.Look.performed   -= OnLook;
        _actions.Player.Look.canceled    -= OnLook;
        
        _actions.Player.VomTag.started   -= OnVomTag;
        
        _actions.Player.Pflug.started    -= OnPflug;
        
        _actions.Player.Alber.started    -= OnAlber;
        
        _actions.Player.Ochs.started     -= OnOchs;
        
        _actions.Player.IronGate.started -= OnIronGate;

        _actions.Player.LeaveGuard.started -= OnLeaveGuard;
        
        _actions.Player.TargetLock.started -= OnTargetLock;

        _actions.Disable();
    }
}