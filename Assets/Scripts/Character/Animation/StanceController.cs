using UnityEngine;

public class StanceController : MonoBehaviour
{
    private AnimationSystem_v2 _animSystem;
    private PlayerInput _input;

    private void Start()
    {
        _animSystem = GetComponent<AnimationSystem_v2>();
        _input = GetComponent<PlayerInput>();
    }

    private void Update()
    {
        if (_input.vomTagTrigger)
        {
            _input.vomTagTrigger = false;
            _animSystem.SetStance("vomTag");
        }
        if (_input.pflugTrigger)
        {
            _input.pflugTrigger = false;
            _animSystem.SetStance("pflug");
        }
        if (_input.alberTrigger)
        {
            _input.alberTrigger = false;
            _animSystem.SetStance("alber");
        }
        if (_input.ochsTrigger)
        {
            _input.ochsTrigger = false;
            _animSystem.SetStance("ochs");
        }
        if (_input.ironGateTrigger)
        {
            _input.ironGateTrigger = false;
            _animSystem.SetStance("ironGate");
        }
        
    }
}