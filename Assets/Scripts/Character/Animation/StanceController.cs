using UnityEngine;

public class StanceController : MonoBehaviour
{
    private AnimationSystem_v2 _animSystem;
    private PlayerInput _input;

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

    private void Update()
    {

        
        if (_animSystem == null || _input == null) return;

        if (_input.vomTagTrigger)
        {
            _input.vomTagTrigger = false;
            SetStance("vomTag");
        }
        if (_input.pflugTrigger)
        {
            _input.pflugTrigger = false;
            SetStance("pflug");
        }
        if (_input.alberTrigger)
        {
            _input.alberTrigger = false;
            SetStance("alber");
        }
        if (_input.ochsTrigger)
        {
            _input.ochsTrigger = false;
            SetStance("ochs");
        }
        if (_input.ironGateTrigger)
        {
            _input.ironGateTrigger = false;
            SetStance("ironGate");
        }

        if (_input.leaveGuardTrigger)
        {
            _input.leaveGuardTrigger = false;
            _animSystem.HideStance();
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