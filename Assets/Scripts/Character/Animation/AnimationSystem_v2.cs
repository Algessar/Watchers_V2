using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationSystem_v2 : MonoBehaviour
{
    private PlayerInput _input;
    private Animator _animator;
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;

    private AnimationMixerPlayable _locomotionMixer;
    private int _idlePort, _walkPort, _runPort;

    private AnimationMixerPlayable _combatMixer;
    private Dictionary<string, int> _stanceNameToPort;
    private int _currentStancePort = -1;
    private int _targetStancePort = -1;
    private float _blendTime;

    //Optional Clip registry
    private Dictionary<string, AnimationClip> _registeredClips = new();


    [Header("Locomotion Clips")] 
    
    [SerializeField] private AnimationClip _idleClip;
    [SerializeField] private AnimationClip _walkClip;
    [SerializeField] private AnimationClip _runClip;
    
    
    private AnimationClipPlayable _idlePlayable;
    private AnimationClipPlayable _walkPlayable;
    private AnimationClipPlayable _runPlayable;

    public bool rootMotion;
    public bool footIk = false;
    public bool handIk = false;

    [Header("Combat Stance Clips")] 
    [SerializeField] private AnimationClip _vomTagClip;
    [SerializeField] private AnimationClip _pflugClip;
    [SerializeField] private AnimationClip _alberClip;
    [SerializeField] private AnimationClip _ochsClip;
    [SerializeField] private AnimationClip _ironGateClip;
    // [SerializeField] private AnimationClip _langortClip; // contextual, not directly triggered

    [Header("Avatar Mask")] [SerializeField]
    private AvatarMask _upperBodyMask; // restricts combat layer to chest, arms, head

    [Header("Settings")] [SerializeField] private float _stanceBlendDuration = 0.02f;
    [Range(0f, 1f)] [SerializeField] private float _runThreshold = 0.6f;

    // Root motion control (off by default)
    public bool EnableRootMotion
    {
        get => _animator.applyRootMotion;
        set => _animator.applyRootMotion = value;
    }

    private void Start()
    {
        _input = GetComponent<PlayerInput>();

        _animator = GetComponentInChildren<Animator>();
        
        
        if (_animator == null) throw new MissingComponentException("Animator required");

        _animator.applyRootMotion = rootMotion;
        
        CreatePlayableGraph();
        // RegisterCombatClips(); //optional, incomplete
    }

    private void Update()
    {
        SetFootIK(_idlePlayable, _walkPlayable, _runPlayable);
        
        _animator.applyRootMotion = rootMotion;

        
        // 1. Update locomotion weights based on movement speed
        float speed = GetMovementSpeed();
        UpdateLocomotionWeights(speed);

        // 2. Handle stance crossfade if in progress
        if (_targetStancePort != -1 && _blendTime < _stanceBlendDuration)
        {
            _blendTime += Time.deltaTime;
            float t = Mathf.Clamp01(_blendTime / _stanceBlendDuration);

            // When blending from no active stance
            if (_currentStancePort == -1)
            {
                _combatMixer.SetInputWeight(_targetStancePort, t);
                _layerMixer.SetInputWeight(1, t);
            }
            else
            {
                _combatMixer.SetInputWeight(_currentStancePort, 1 - t);
                _combatMixer.SetInputWeight(_targetStancePort, t);
                _layerMixer.SetInputWeight(1, 1f);
            }

            if (t >= 1f)
            {
                // Blend finished

                if (_currentStancePort != -1 && _currentStancePort != _targetStancePort)
                {
                    _combatMixer.SetInputWeight(_currentStancePort, 0f);
                }

                _currentStancePort = _targetStancePort;
                _targetStancePort = -1;
            }
        }
    }

    private void UpdateLocomotionWeights(float speed)
    {
        float walkWeight = 0f, runWeight = 0f;

        if (speed <= _runThreshold)
        {
            float t = _runThreshold > 0 ? speed / _runThreshold : 0f;
            _locomotionMixer.SetInputWeight(_idlePort, 1 - t);
            walkWeight = t;
        }
        else
        {
            float t = (speed - _runThreshold) / (1f - _runThreshold);
            walkWeight = 1 - t;
            runWeight = t;
        }

        _locomotionMixer.SetInputWeight(_walkPort, walkWeight);
        _locomotionMixer.SetInputWeight(_runPort, runWeight);
    }

    private float GetMovementSpeed()
    {
        //TODO: change to GetComponent in Start
        return _input != null ? _input.moveAmount : 0f;
    }

    public void SetFootIK( params AnimationClipPlayable[] clipPlayable)
    {
        for (int i = 0; i < clipPlayable.Length; i++)
        {
            
            clipPlayable[i].SetApplyFootIK(false);
        }
        
    }


    private void CreatePlayableGraph()
    {
        _graph = PlayableGraph.Create("AnimationSystem");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime); //What is this?

        var output = AnimationPlayableOutput.Create(_graph, "Output", _animator);

        // Layer mixer (mask): 2 layers (0 = locomotion, 1 = combat)
        _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);

        // --- Layer 0: Locomotion (full body) ---------------------------------
        _locomotionMixer = AnimationMixerPlayable.Create(_graph, 3);
        _idlePort = 0;
        _walkPort = 1;
        _runPort = 2;

        _idlePlayable = AnimationClipPlayable.Create(_graph, _idleClip);
        _walkPlayable = AnimationClipPlayable.Create(_graph, _walkClip);
        _runPlayable = AnimationClipPlayable.Create(_graph, _runClip);


        _graph.Connect(_idlePlayable, 0, _locomotionMixer, _idlePort);
        _graph.Connect(_walkPlayable, 0, _locomotionMixer, _walkPort);
        _graph.Connect(_runPlayable, 0, _locomotionMixer, _runPort);

        _locomotionMixer.SetInputWeight(_idlePort, 1f);
        _locomotionMixer.SetInputWeight(_walkPort, 0f);
        _locomotionMixer.SetInputWeight(_runPort, 0f);

        _layerMixer.ConnectInput(0, _locomotionMixer, 0, 1f);
        _layerMixer.SetLayerMaskFromAvatarMask(0, _upperBodyMask);

        int stanceCount = 5;
        _combatMixer = AnimationMixerPlayable.Create(_graph, stanceCount);
        _stanceNameToPort = new();

        void AttachStance(AnimationClip clip, string name, int port)
        {
            if (clip == null) return;
            var playable = AnimationClipPlayable.Create(_graph, clip);
            _graph.Connect(playable, 0, _combatMixer, port);
            _stanceNameToPort[name] = port;
            _combatMixer.SetInputWeight(port, 0f);
        }

        AttachStance(_vomTagClip, "vomTag", 0);
        AttachStance(_pflugClip, "pflug", 1);
        AttachStance(_alberClip, "alber", 2);
        AttachStance(_ochsClip, "ochs", 3);
        AttachStance(_ironGateClip, "ironGate", 4);

        _layerMixer.ConnectInput(1, _combatMixer, 0, 1f);
        if (_upperBodyMask != null)
        {
            _layerMixer.SetLayerMaskFromAvatarMask(1, _upperBodyMask);
        }

        _layerMixer.SetInputWeight(1, 0f);
        _currentStancePort = -1;
        _targetStancePort = -1;

        output.SetSourcePlayable(_layerMixer);
        _graph.Play();
    }


    // ------------------- Public API ---------------------

    /// <summary> Transition to a stance by name. </summary>
    public void SetStance(string stanceName)
    {
        if (!_stanceNameToPort.TryGetValue(stanceName, out int targetPort))
        {
            Debug.LogWarning($"Stance '{stanceName} not registered");
            return;
        }

        if (_targetStancePort == targetPort) return;

        // If already in that stance and fully blended, do nothing
        if (_currentStancePort == targetPort && _targetStancePort == -1) return;

        // Complete previous blend instantly if needed (why, Deek?)
        if (_targetStancePort != -1 && _blendTime < _stanceBlendDuration)
        {
            if (_currentStancePort != -1)
            {
                _combatMixer.SetInputWeight(_currentStancePort, 0f);
            }

            _combatMixer.SetInputWeight(_targetStancePort, 1f);
            _currentStancePort = _targetStancePort;
            _layerMixer.SetInputWeight(1, 1f);
        }

        _targetStancePort = targetPort;
        _blendTime = 0f;
    }

    public void HideStance()
    {
        if (_targetStancePort != -1 && _blendTime < _stanceBlendDuration)
        {
            // Cancel pending blend
            if (_currentStancePort != -1)
            {
                _combatMixer.SetInputWeight(_currentStancePort, 0f);
            }

            _combatMixer.SetInputWeight(_targetStancePort, 0f);
            _targetStancePort = -1;
        }

        if (_currentStancePort != -1)
        {
            _currentStancePort = -1;
            _targetStancePort = -1;
            _blendTime = 0f;
        }
    }

    /// <summary> Register a clip dynamically (optional). </summary>
    public void RegisterClip(string id, AnimationClip clip, bool isCombatClip = true)
    {
        _registeredClips[id] = clip;
        if (isCombatClip)
        {
            // Extend combat mixer if needed – for simplicity we require pre‑allocated slots.
            // This example assumes you will add slots beforehand; otherwise you can implement
            // dynamic mixer resizing (more complex). For KISS, we just store for later use.
            Debug.Log($"Clip '{id}' registered but dynamic adding to mixer not implemented. Use inspector fields instead.");
        }
    }

    public AnimationClip GetRegisteredClip(string id)
    {
        _registeredClips.TryGetValue(id, out var clip);
        return clip;
    }

    private void RegisterCombatClips()
    {
        RegisterClip("vomTag",   _vomTagClip,   true);
        RegisterClip("pflug",    _pflugClip,    true);
        RegisterClip("alber",    _alberClip,    true);
        RegisterClip("ochs",     _ochsClip,     true);
        RegisterClip("ironGate", _ironGateClip, true);
    }

    private void OnDestroy()
    {
        if (_graph.IsValid())
            _graph.Destroy();
    }
}