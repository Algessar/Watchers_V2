using System;
using System.Collections.Generic;
using UnityEngine;

public enum StanceType
{
    VomTag,
    Alber,
    Pflug,
    Ochs,
    IronGate,
    Langort,
    
}



public class StanceController : MonoBehaviour
{
    private AnimationSystem_v2 _animSystem;
    private PlayerInput _input;

    public Action OnVomTag;
    public Action OnPflug;
    public Action OnAlber;
    public Action OnOchs;
    public Action OnIronGate;

    public Action OnLeaveGuard;

    public Stance stance;


    // private static AnimationClipPlayable clip;

    // private Dictionary<StanceType, Func<Stance, Stance>> _transitions = new Dictionary<StanceType, Func<Stance, Stance>>()
    // {
    //     { StanceType.VomTag , b => new Stance("", clip)}
    // };


    [SerializeField] private bool _debugStanceInput = true;

    private void Start()
    {
        _animSystem = GetComponent<AnimationSystem_v2>();
        _input = GetComponent<PlayerInput>();

        if (_animSystem == null)
        {
            Debug.LogError("[StanceController] AnimationSystem_v2 component is missing; stance inputs cannot drive animations.", this);
        }

        if (_input == null)
        {
            Debug.LogError("[StanceController] PlayerInput component is missing; stance inputs cannot be read.", this);
            
        }
        
       
    }

    bool IsInGuard()
    {
        
        
        
        return false;
    }

    private void Update()
    {

        
        if (_animSystem == null || _input == null) return;

        if (_input.vomTagTrigger)
        {
            _input.vomTagTrigger = false;
            OnVomTag?.Invoke();
            SetStance("vomTag");
        }
        if (_input.pflugTrigger)
        {
            _input.pflugTrigger = false;
            OnPflug?.Invoke();
            SetStance("pflug");
        }
        if (_input.alberTrigger)
        {
            _input.alberTrigger = false;
            OnAlber?.Invoke();
            SetStance("alber");
        }
        if (_input.ochsTrigger)
        {
            _input.ochsTrigger = false;
            OnOchs?.Invoke();
            SetStance("ochs");
        }
        if (_input.ironGateTrigger)
        {
            _input.ironGateTrigger = false;
            OnIronGate?.Invoke();
            SetStance("ironGate");
        }

        if (_input.leaveGuardTrigger)
        {
            _input.leaveGuardTrigger = false;
            OnLeaveGuard?.Invoke();
            _animSystem.HideStance();
        }
    }

    public void Transition(string from, string to)
    {
        if (from == "vomTag")
        {
            if (to == "  ")
            {
                
            }
            else if (to == "  ")
            {
                
            }
            else if (to == "  ")
            {
                
            }
            else if (to == "  ")
            {
                
            }
        }
        
        else if (from == "ochs")
        {
            // Transition to VomTag then Langort
        }
        
        else if (from == "pflug")
        {

            if (to == "alber")
            {
                // Langort
                // NOTE: Should this go like, halfway to VomTag before langort? Then landing in Alber.
            }
            else if (to == "langort")
            {
                // Thrust
            }
            else if (to == "ochs")
            {
                // Interpolate
            }
            else if (to == "")
            {
                
            }
            
        }
        
        else if (from == "alber")
        {

            if (to == "pflug" || to == "alber")
            {
                // Transition to Langort
            }
            else if (to == "ochs")
            {
                // Transition to zwerchau
            }
            else if (to == "langort")
            {
                //Perform thrust
            }

        }
        
        else if (from == "vomTag")
        {
            // Transition to Langort
            
        }
    }

    private void SetStance(string stanceName)
    {
        if (_debugStanceInput)
        {
            Debug.Log($"[StanceController] Triggered stance '{stanceName}' with moveAmount={_input.moveAmount:F3}.", this);
        }

        _animSystem.SetStance(stanceName);
    }
}