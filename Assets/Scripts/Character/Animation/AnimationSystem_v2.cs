using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;
#if UNITY_EDITOR
using UnityEditor;
#endif

public class AnimationSystem_v2 : MonoBehaviour
{
    private const int LocomotionLayer = 0;
    private const int CombatLayer = 1;

    private PlayerInput _input;
    private Animator _animator;
    private PlayableGraph _graph;
    private AnimationLayerMixerPlayable _layerMixer;

    private AnimationMixerPlayable _locomotionMixer;
    private int _idlePort, _walkPort, _runPort;

    private AnimationMixerPlayable _combatMixer;
    private Dictionary<string, int> _stanceNameToPort;
    private readonly Dictionary<int, string> _stancePortToName = new();
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

    [Header("Avatar Mask")]
    [SerializeField] private AvatarMask _upperBodyMask; // restricts combat layer to chest, arms, head

    [Header("Settings")]
    [SerializeField] private float _stanceBlendDuration = 0.02f;
    [Range(0f, 1f)] [SerializeField] private float _runThreshold = 0.6f;

    [Header("Clip Sanitization")]
    [Tooltip("When running in the Unity Editor, clone assigned clips and remove scale curves before they enter the playable graph. This is useful for Generic rigs where imported scale bindings can stretch bones even when the source rig looks clean.")]
    [SerializeField] private bool _stripScaleCurvesFromPlayableClips = true;
    [SerializeField] private int _maxScaleCurveSamplesToLog = 8;

    [Header("Debug")]
    [SerializeField] private bool _debugAnimationWeights = true;
    [SerializeField] private float _debugLogInterval = 0.5f;
    [Tooltip("Optional transform to use as the root when logging armature scales. Defaults to the Animator transform.")]
    [SerializeField] private Transform _debugScaleProbeRoot;

    private float _nextDebugLogTime;
    private string _lastDebugSignature;
    private Transform _debugHips;
    private Transform _debugLeftThigh;
    private Transform _debugLeftFoot;
    private Transform _debugSpine;
    private readonly List<AnimationClip> _runtimeClipCopies = new();

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
        CacheDebugScaleProbeBones();

        CreatePlayableGraph();
        // RegisterCombatClips(); //optional, incomplete
    }

    private void Update()
    {
        _animator.applyRootMotion = rootMotion;

        // 1. Update locomotion weights based on movement speed
        float speed = GetMovementSpeed();
        UpdateLocomotionWeights(speed);

        // 2. Handle stance crossfade if in progress
        if (_targetStancePort != -1 && _blendTime < _stanceBlendDuration)
        {
            _blendTime += Time.deltaTime;
            float t = _stanceBlendDuration > 0f ? Mathf.Clamp01(_blendTime / _stanceBlendDuration) : 1f;

            // When blending from no active stance
            if (_currentStancePort == -1)
            {
                _combatMixer.SetInputWeight(_targetStancePort, t);
                _layerMixer.SetInputWeight(CombatLayer, t);
            }
            else
            {
                _combatMixer.SetInputWeight(_currentStancePort, 1 - t);
                _combatMixer.SetInputWeight(_targetStancePort, t);
                _layerMixer.SetInputWeight(CombatLayer, 1f);
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

        LogAnimationDebugIfNeeded(speed);
    }

    private void UpdateLocomotionWeights(float speed)
    {
        float clampedSpeed = Mathf.Clamp01(speed);
        float idleWeight = 0f, walkWeight = 0f, runWeight = 0f;

        if (clampedSpeed <= _runThreshold)
        {
            float t = _runThreshold > 0f ? clampedSpeed / _runThreshold : 0f;
            idleWeight = 1 - t;
            walkWeight = t;
        }
        else
        {
            float t = _runThreshold < 1f ? (clampedSpeed - _runThreshold) / (1f - _runThreshold) : 1f;
            walkWeight = 1 - t;
            runWeight = t;
        }

        _locomotionMixer.SetInputWeight(_idlePort, idleWeight);
        _locomotionMixer.SetInputWeight(_walkPort, walkWeight);
        _locomotionMixer.SetInputWeight(_runPort, runWeight);
    }

    private float GetMovementSpeed()
    {
        //TODO: change to GetComponent in Start
        return _input != null ? _input.moveAmount : 0f;
    }

    public void SetFootIK(params AnimationClipPlayable[] clipPlayable)
    {
        for (int i = 0; i < clipPlayable.Length; i++)
        {
            if (clipPlayable[i].IsValid())
            {
                clipPlayable[i].SetApplyFootIK(footIk);
                clipPlayable[i].SetApplyPlayableIK(handIk);
            }
        }
    }


    private void CreatePlayableGraph()
    {
        _graph = PlayableGraph.Create("AnimationSystem");
        _graph.SetTimeUpdateMode(DirectorUpdateMode.GameTime); // Advances from Unity's game-time update loop.

        var output = AnimationPlayableOutput.Create(_graph, "Output", _animator);

        // Layer mixer: layer 0 = unmasked full-body locomotion, layer 1 = masked upper-body combat.
        _layerMixer = AnimationLayerMixerPlayable.Create(_graph, 2);

        // --- Layer 0: Locomotion (full body) ---------------------------------
        _locomotionMixer = AnimationMixerPlayable.Create(_graph, 3);
        _idlePort = 0;
        _walkPort = 1;
        _runPort = 2;

        _idlePlayable = CreateClipPlayable(_idleClip, "Idle");
        _walkPlayable = CreateClipPlayable(_walkClip, "Walk");
        _runPlayable = CreateClipPlayable(_runClip, "Run");
        SetFootIK(_idlePlayable, _walkPlayable, _runPlayable);

        ConnectClipPlayable(_idlePlayable, _locomotionMixer, _idlePort, "Idle");
        ConnectClipPlayable(_walkPlayable, _locomotionMixer, _walkPort, "Walk");
        ConnectClipPlayable(_runPlayable, _locomotionMixer, _runPort, "Run");

        _locomotionMixer.SetInputWeight(_idlePort, 1f);
        _locomotionMixer.SetInputWeight(_walkPort, 0f);
        _locomotionMixer.SetInputWeight(_runPort, 0f);

        _layerMixer.ConnectInput(LocomotionLayer, _locomotionMixer, 0, 1f);
        _layerMixer.SetInputWeight(LocomotionLayer, 1f);
        // Do not apply the upper-body mask to locomotion. The base layer must remain full-body,
        // otherwise the legs have no source to play walk/run once the combat layer is masked.

        int stanceCount = 5;
        _combatMixer = AnimationMixerPlayable.Create(_graph, stanceCount);
        _stanceNameToPort = new();
        _stancePortToName.Clear();

        void AttachStance(AnimationClip clip, string name, int port)
        {
            if (clip == null)
            {
                Debug.LogWarning($"[AnimationSystem_v2] Stance clip '{name}' is not assigned; SetStance('{name}') will be ignored.", this);
                return;
            }

            var playable = CreateClipPlayable(clip, name);
            ConnectClipPlayable(playable, _combatMixer, port, name);
            _stanceNameToPort[name] = port;
            _stancePortToName[port] = name;
            _combatMixer.SetInputWeight(port, 0f);
        }

        AttachStance(_vomTagClip, "vomTag", 0);
        AttachStance(_pflugClip, "pflug", 1);
        AttachStance(_alberClip, "alber", 2);
        AttachStance(_ochsClip, "ochs", 3);
        AttachStance(_ironGateClip, "ironGate", 4);

        _layerMixer.ConnectInput(CombatLayer, _combatMixer, 0, 1f);
        if (_upperBodyMask != null)
        {
            _layerMixer.SetLayerMaskFromAvatarMask(CombatLayer, _upperBodyMask);
        }
        else
        {
            Debug.LogWarning("[AnimationSystem_v2] UpperBodyMask is not assigned. Combat stances will override the full body and can block locomotion legs.", this);
        }

        _layerMixer.SetInputWeight(CombatLayer, 0f);
        _currentStancePort = -1;
        _targetStancePort = -1;

        output.SetSourcePlayable(_layerMixer);
        _graph.Play();

        LogClipDiagnostics();
        LogAnimationDebug("graph created", GetMovementSpeed(), force: true);
    }

    private AnimationClipPlayable CreateClipPlayable(AnimationClip clip, string label)
    {
        if (clip == null)
        {
            Debug.LogError($"[AnimationSystem_v2] Required animation clip '{label}' is not assigned.", this);
            return default(AnimationClipPlayable);
        }

        return AnimationClipPlayable.Create(_graph, GetPlayableClip(clip, label));
    }

    private AnimationClip GetPlayableClip(AnimationClip clip, string label)
    {
#if UNITY_EDITOR
        if (!_stripScaleCurvesFromPlayableClips) return clip;

        int scaleCurveCount = CountScaleCurveBindings(clip);
        if (scaleCurveCount == 0) return clip;

        AnimationClip sanitizedClip = Instantiate(clip);
        sanitizedClip.name = $"{clip.name}_ScaleCurvesStripped";
        int removedCurveCount = RemoveScaleCurves(sanitizedClip);
        _runtimeClipCopies.Add(sanitizedClip);

        Debug.Log($"[AnimationSystem_v2] Clip '{label}' has {scaleCurveCount} scale curve bindings. Using runtime clone '{sanitizedClip.name}' with {removedCurveCount} scale curves removed before creating its playable.", this);
        return sanitizedClip;
#else
        if (_stripScaleCurvesFromPlayableClips && _debugAnimationWeights)
        {
            Debug.LogWarning($"[AnimationSystem_v2] Scale-curve stripping for clip '{label}' requires UnityEditor APIs and only runs in the Editor. Check import settings or pre-strip clips for player builds.", this);
        }

        return clip;
#endif
    }

    private void ConnectClipPlayable(AnimationClipPlayable playable, AnimationMixerPlayable mixer, int port, string label)
    {
        if (!playable.IsValid())
        {
            Debug.LogError($"[AnimationSystem_v2] Cannot connect invalid playable for '{label}' on port {port}.", this);
            return;
        }

        _graph.Connect(playable, 0, mixer, port);
    }


    // ------------------- Public API ---------------------

    /// <summary> Transition to a stance by name. </summary>
    public void SetStance(string stanceName)
    {
        if (!_stanceNameToPort.TryGetValue(stanceName, out int targetPort))
        {
            Debug.LogWarning($"Stance '{stanceName}' not registered", this);
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
            _layerMixer.SetInputWeight(CombatLayer, 1f);
        }

        _targetStancePort = targetPort;
        _blendTime = 0f;
        LogAnimationDebug($"SetStance('{stanceName}')", GetMovementSpeed(), force: true);
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
            _combatMixer.SetInputWeight(_currentStancePort, 0f);
            _layerMixer.SetInputWeight(CombatLayer, 0f);
            _currentStancePort = -1;
            _targetStancePort = -1;
            _blendTime = 0f;
        }

        LogAnimationDebug("HideStance", GetMovementSpeed(), force: true);
    }

    /// <summary> Register a clip dynamically (optional). </summary>
    public void RegisterClip(string id, AnimationClip clip, bool isCombatClip = true)
    {
        _registeredClips[id] = clip;
        if (isCombatClip)
        {
            // Extend combat mixer if needed – for simplicity we require pre-allocated slots.
            // This example assumes you will add slots beforehand; otherwise you can implement
            // dynamic mixer resizing (more complex). For KISS, we just store for later use.
            Debug.Log($"Clip '{id}' registered but dynamic adding to mixer not implemented. Use inspector fields instead.", this);
        }
    }

    public AnimationClip GetRegisteredClip(string id)
    {
        _registeredClips.TryGetValue(id, out var clip);
        return clip;
    }

    private void RegisterCombatClips()
    {
        RegisterClip("vomTag", _vomTagClip, true);
        RegisterClip("pflug", _pflugClip, true);
        RegisterClip("alber", _alberClip, true);
        RegisterClip("ochs", _ochsClip, true);
        RegisterClip("ironGate", _ironGateClip, true);
    }

    private void LogAnimationDebugIfNeeded(float speed)
    {
        if (!_debugAnimationWeights || Time.time < _nextDebugLogTime) return;

        string signature = $"{_layerMixer.GetInputWeight(LocomotionLayer):F2}|{_layerMixer.GetInputWeight(CombatLayer):F2}|" +
                           $"{_locomotionMixer.GetInputWeight(_idlePort):F2}|{_locomotionMixer.GetInputWeight(_walkPort):F2}|{_locomotionMixer.GetInputWeight(_runPort):F2}|" +
                           $"{_currentStancePort}|{_targetStancePort}|{speed:F2}|{ScaleSignature(_debugHips)}|{ScaleSignature(_debugLeftThigh)}|{ScaleSignature(_debugLeftFoot)}";

        if (signature == _lastDebugSignature) return;

        _lastDebugSignature = signature;
        LogAnimationDebug("update", speed, force: false);
    }

    private void LogAnimationDebug(string reason, float speed, bool force)
    {
        if (!_debugAnimationWeights && !force) return;

        _nextDebugLogTime = Time.time + Mathf.Max(0.05f, _debugLogInterval);

        var builder = new StringBuilder();
        builder.Append($"[AnimationSystem_v2] {reason} | speed={speed:F3} rootMotion={_animator.applyRootMotion} avatar={_animator.avatar?.name ?? "<none>"} ");
        builder.Append($"graphValid={_graph.IsValid()} graphPlaying={(_graph.IsValid() && _graph.IsPlaying())} ");
        builder.Append($"layerWeights locomotion={_layerMixer.GetInputWeight(LocomotionLayer):F3} combat={_layerMixer.GetInputWeight(CombatLayer):F3} ");
        builder.Append($"locomotionWeights idle={_locomotionMixer.GetInputWeight(_idlePort):F3} walk={_locomotionMixer.GetInputWeight(_walkPort):F3} run={_locomotionMixer.GetInputWeight(_runPort):F3} ");
        builder.Append($"stance current={GetStanceLabel(_currentStancePort)} target={GetStanceLabel(_targetStancePort)} blend={_blendTime:F3}/{_stanceBlendDuration:F3} ");
        builder.Append("combatWeights=");
        AppendMixerWeights(builder, _combatMixer, _stancePortToName);
        builder.Append(" scales=");
        AppendScale(builder, "animator", _animator.transform);
        AppendScale(builder, "hips", _debugHips);
        AppendScale(builder, "spine", _debugSpine);
        AppendScale(builder, "leftThigh", _debugLeftThigh);
        AppendScale(builder, "leftFoot", _debugLeftFoot);

        Debug.Log(builder.ToString(), this);
    }

    private void AppendMixerWeights(StringBuilder builder, AnimationMixerPlayable mixer, Dictionary<int, string> labels)
    {
        builder.Append('[');
        for (int i = 0; i < mixer.GetInputCount(); i++)
        {
            if (i > 0) builder.Append(", ");
            labels.TryGetValue(i, out string label);
            builder.Append(label ?? $"port{i}");
            builder.Append('=');
            builder.Append(mixer.GetInputWeight(i).ToString("F3"));
        }
        builder.Append(']');
    }

    private string GetStanceLabel(int port)
    {
        if (port == -1) return "<none>";
        return _stancePortToName.TryGetValue(port, out string label) ? $"{label}({port})" : $"port{port}";
    }

    private void CacheDebugScaleProbeBones()
    {
        Transform root = _debugScaleProbeRoot != null ? _debugScaleProbeRoot : _animator.transform;
        _debugHips = FindDescendantByNameContains(root, "pelvis") ?? FindDescendantByNameContains(root, "hip");
        _debugLeftThigh = FindDescendantByNameContains(root, "thigh.l") ?? FindDescendantByNameContains(root, "thigh_l") ?? FindDescendantByNameContains(root, "leftthigh");
        _debugLeftFoot = FindDescendantByNameContains(root, "foot.l") ?? FindDescendantByNameContains(root, "foot_l") ?? FindDescendantByNameContains(root, "leftfoot");
        _debugSpine = FindDescendantByNameContains(root, "spine.003") ?? FindDescendantByNameContains(root, "chest") ?? FindDescendantByNameContains(root, "spine");
    }

    private Transform FindDescendantByNameContains(Transform root, string fragment)
    {
        if (root == null || string.IsNullOrEmpty(fragment)) return null;

        string lowerFragment = fragment.ToLowerInvariant();
        foreach (Transform child in root.GetComponentsInChildren<Transform>())
        {
            if (child.name.ToLowerInvariant().Contains(lowerFragment)) return child;
        }

        return null;
    }

    private void AppendScale(StringBuilder builder, string label, Transform transform)
    {
        builder.Append(' ');
        builder.Append(label);
        builder.Append('=');
        builder.Append(ScaleSignature(transform));
    }

    private string ScaleSignature(Transform transform)
    {
        if (transform == null) return "<missing>";
        Vector3 scale = transform.localScale;
        return $"({scale.x:F3},{scale.y:F3},{scale.z:F3})";
    }

    private void LogClipDiagnostics()
    {
        if (!_debugAnimationWeights) return;

        LogClipDiagnostics(_idleClip, "Idle");
        LogClipDiagnostics(_walkClip, "Walk");
        LogClipDiagnostics(_runClip, "Run");
        LogClipDiagnostics(_vomTagClip, "vomTag");
        LogClipDiagnostics(_pflugClip, "pflug");
        LogClipDiagnostics(_alberClip, "alber");
        LogClipDiagnostics(_ochsClip, "ochs");
        LogClipDiagnostics(_ironGateClip, "ironGate");
    }

    private void LogClipDiagnostics(AnimationClip clip, string label)
    {
        if (clip == null) return;

        string scaleCurveInfo = "runtime binding details unavailable";
#if UNITY_EDITOR
        int scaleCurveCount = CountScaleCurveBindings(clip);
        string scaleCurveSamples = GetScaleCurveSamples(clip, _maxScaleCurveSamplesToLog);
        scaleCurveInfo = $"scaleCurveBindings={scaleCurveCount} stripScaleCurves={_stripScaleCurvesFromPlayableClips} samples=[{scaleCurveSamples}]";
#endif
        Debug.Log($"[AnimationSystem_v2] Clip '{label}' name='{clip.name}' length={clip.length:F3}s frameRate={clip.frameRate:F1} wrapMode={clip.wrapMode} {scaleCurveInfo}", this);
    }

#if UNITY_EDITOR
    private int CountScaleCurveBindings(AnimationClip clip)
    {
        int scaleCurveCount = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (IsScaleCurveBinding(binding)) scaleCurveCount++;
        }

        return scaleCurveCount;
    }

    private int RemoveScaleCurves(AnimationClip clip)
    {
        int removedCurveCount = 0;
        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!IsScaleCurveBinding(binding)) continue;

            AnimationUtility.SetEditorCurve(clip, binding, null);
            removedCurveCount++;
        }

        return removedCurveCount;
    }

    private string GetScaleCurveSamples(AnimationClip clip, int maxSamples)
    {
        if (maxSamples <= 0) return string.Empty;

        var builder = new StringBuilder();
        int sampleCount = 0;
        int totalScaleCurveCount = 0;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!IsScaleCurveBinding(binding)) continue;

            totalScaleCurveCount++;
            if (sampleCount >= maxSamples) continue;

            if (builder.Length > 0) builder.Append(", ");
            builder.Append(binding.path);
            builder.Append(':');
            builder.Append(binding.propertyName);
            sampleCount++;
        }

        if (totalScaleCurveCount > sampleCount)
        {
            if (builder.Length > 0) builder.Append(", ");
            builder.Append("...");
        }

        return builder.ToString();
    }

    private bool IsScaleCurveBinding(EditorCurveBinding binding)
    {
        return binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0;
    }
#endif

    private void OnDestroy()
    {
        if (_graph.IsValid())
            _graph.Destroy();

        for (int i = 0; i < _runtimeClipCopies.Count; i++)
        {
            if (_runtimeClipCopies[i] != null)
            {
                Destroy(_runtimeClipCopies[i]);
            }
        }

        _runtimeClipCopies.Clear();
    }
}