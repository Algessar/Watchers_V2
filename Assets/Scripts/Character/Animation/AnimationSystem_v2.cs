using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationSystem_v2 : MonoBehaviour
{
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


    [Header("Locomotion Clips")] [SerializeField]
    private AnimationClip _idleClip;

    [SerializeField] private AnimationClip _walkClip;
    [SerializeField] private AnimationClip _runClip;

    [Header("Combat Stance Clips")] [SerializeField]
    private AnimationClip _vomTagClip;

    [SerializeField] private AnimationClip _pflugClip;
    [SerializeField] private AnimationClip _alberClip;
    [SerializeField] private AnimationClip _ochsClip;
    [SerializeField] private AnimationClip _ironGateClip;
    [SerializeField] private AnimationClip _langortClip; // contextual, not directly triggered

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
        _animator = GetComponentInChildren<Animator>();
        if (_animator == null) throw new MissingComponentException("Animator required");

        _animator.applyRootMotion = false;

        CreatePlayableGraph();
        RegisterCombatClips(); //optional, incomplete
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

        var idlePlayable = AnimationClipPlayable.Create(_graph, _idleClip);
        var walkPlayable = AnimationClipPlayable.Create(_graph, _walkClip);
        var runPlayable = AnimationClipPlayable.Create(_graph, _runClip);

        _graph.Connect(idlePlayable, 0, _locomotionMixer, _idlePort);
        _graph.Connect(walkPlayable, 0, _locomotionMixer, _walkPort);
        _graph.Connect(runPlayable, 0, _locomotionMixer, _runPort);

        _locomotionMixer.SetInputWeight(_idlePort, 1f);
        _locomotionMixer.SetInputWeight(_walkPort, 0f);
        _locomotionMixer.SetInputWeight(_runPort, 0f);

        _layerMixer.ConnectInput(0, _locomotionMixer, 0, 1f);
        _layerMixer.SetLayerMaskFromAvatarMask(0, null);

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

        AttachStance(_vomTagClip,   "vomTag",   0);
        AttachStance(_pflugClip,    "pflug",    1);
        AttachStance(_alberClip,    "alber",    2);
        AttachStance(_ochsClip,     "ochs",     3);
        AttachStance(_ironGateClip, "ironGate", 4);

        _layerMixer.ConnectInput(1, _combatMixer, 0, 1f);
        if (_upperBodyMask != null)
        {
            _layerMixer.SetLayerMaskFromAvatarMask(1, _upperBodyMask);
        }
        
        _layerMixer.SetInputWeight(1, 0f);
        _currentStancePort = -1;
        _targetStancePort  = -1;
        
        output.SetSourcePlayable(_layerMixer);
        _graph.Play();
    }

    private void RegisterCombatClips()
    {
        throw new NotImplementedException();
    }
}