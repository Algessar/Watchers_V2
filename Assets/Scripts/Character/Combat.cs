using System;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class Combat : MonoBehaviour
{
    
    
    private Animator _animator;
    private PlayerInput _input;

    private BoneCollector _bones;

    private PlayableGraph _graph;
    private AnimationMixerPlayable _mixer;
    

    private AnimationClipPlayable _vomTagPlayable;
    private AnimationClipPlayable _pflugPlayable;
    private AnimationClipPlayable _alberPlayable;
    private AnimationClipPlayable _ochsPlayable;
    private AnimationClipPlayable _ironGatePlayable;
    
    private AnimationClipPlayable _langortPlayable;
    

    
    public AnimationClip  vomTagClip ;
    public AnimationClip   pflugClip ;
    public AnimationClip   alberClip ;
    public AnimationClip    ochsClip ;
    public AnimationClip ironGateClip;
    
    //Special case. Not manually triggered.
    public AnimationClip langortClip ;
    

    private int _currentIndex; //Whichever guard is current
    private int _targetIndex; //Whichever guard is transitioned to

    private float _time;
    private float _blendTime;
    [SerializeField] private float _blendDuration = 0.2f; //TODO: remember where this is and fix for actual anim length.

    private void OnEnable()
    {
        _currentIndex = 0;
    }


    private void Start()
    {
        _animator = GetComponentInChildren<Animator>();

        _animator.applyRootMotion = false;
        
        _input = GetComponent<PlayerInput>();
        _bones = GetComponent<BoneCollector>();
        
        _graph = PlayableGraph.Create();

        var output = AnimationPlayableOutput.Create(
            _graph,
            "Animation",
            _animator);

        _mixer = AnimationMixerPlayable.Create(_graph, 5);
        
        _vomTagPlayable   = AnimationClipPlayable.Create(_graph, vomTagClip);
        _pflugPlayable    = AnimationClipPlayable.Create(_graph, pflugClip);
        _alberPlayable    = AnimationClipPlayable.Create(_graph, alberClip);
        _ochsPlayable     = AnimationClipPlayable.Create(_graph, ochsClip);
        _ironGatePlayable = AnimationClipPlayable.Create(_graph, ironGateClip);
        

        _graph.Connect(_vomTagPlayable, 0, _mixer, 0);
        _graph.Connect(_pflugPlayable, 0, _mixer, 1);
        _graph.Connect(_alberPlayable, 0, _mixer, 2);
        _graph.Connect(_ochsPlayable, 0, _mixer, 3);
        _graph.Connect(_ironGatePlayable, 0, _mixer, 4);
       

        _mixer.SetInputWeight(0, 1);

        output.SetSourcePlayable(_mixer);

        _graph.Play();
        
        for (int i = 0; i < _mixer.GetInputCount(); i++)
        {
            // _mixer.SetInputWeight(i, 0);
            
            _mixer.SetInputWeight(i, i == _currentIndex ? 1f : 0f);
        }
    }

    private void Update()
    {
        TriggerGuards();

        if (_blendTime < _blendDuration)
        {
            _blendTime += Time.deltaTime;
            float t = Mathf.Clamp01(_blendTime / _blendDuration);

            // Smoothly interpolate weight between current and target
            _mixer.SetInputWeight(_currentIndex, 1 - t);
            _mixer.SetInputWeight(_targetIndex, t);
        }
        
        //NOTE: Test for index return
        if (_blendTime >= _blendDuration && _currentIndex != _targetIndex)
        {
            // Blend finished – the active clip is now _targetIndex
            _currentIndex = _targetIndex;
        }
    }

    void FadeTo(int index)
    {
        if (_targetIndex == index) return;

        // Complete any ongoing blend first
        if (_blendTime < _blendDuration)
        {
            // Snap to the target clip
            _mixer.SetInputWeight(_currentIndex, 0);
            _mixer.SetInputWeight(_targetIndex, 1);
        }

        _currentIndex = _targetIndex;
        _targetIndex = index;
        _blendTime = 0;
    }

    void TriggerVomTag()
    {
        if (vomTagClip == null)
        {
            Debug.Log("VomTag input pressed but no clip");
            // FadeTo(0);//NOTE: Temp
            
            return;
        }

        FadeTo(0);

    }

    void TriggerPflug()
    {
        if (pflugClip == null)
        {
            Debug.Log("Pflug input pressed but no clip");
            // FadeTo(1); //NOTE: Temp

            return;
        }
        FadeTo(1);
    }
    
    void TriggerAlber()
    {
        if (alberClip == null)
        {
            Debug.Log("Alber input pressed but no clip");
            // FadeTo(1); //NOTE: Temp

            return;
        }
        FadeTo(2);

    }
    
    void TriggerOchs()
    {
        if (ochsClip == null)
        {
            Debug.Log("Ochs input pressed but no clip");
            // FadeTo(1); //NOTE: Temp

            return;
        }
        FadeTo(3);

    }
    
    // OOPS. This should not be a direct input
    void TriggerIronGate()
    {
        if (ironGateClip == null)
        {
            Debug.Log("IronGate input pressed but no clip");
            // FadeTo(1); //NOTE: Temp

            return;
        }
        FadeTo(4);
    }

    void TriggerGuards()
    {
        
        if (_input.vomTagTrigger)
        {
            //BUTTON A
            _input.vomTagTrigger = false;
            Debug.Log("Attempting to switch to VomTag (A): index 0.");

            // if (_currentIndex == 0)
            // {
            //     Debug.Log("Already in VomTag, returning.");
            //     return;
            // }

            TriggerVomTag();
        }
        if(_input.pflugTrigger)
        {
            //BUTTON B?

            _input.pflugTrigger = false;
            
            Debug.Log("Attempting to switch to Pflug (B): index 1.");

            // if (_currentIndex == 1) return;

            
            TriggerPflug();
        }
        if (_input.alberTrigger)
        {
            _input.alberTrigger = false;
            
            Debug.Log("Attempting to switch to Alber (?): index 2.");

            
            // if (_currentIndex == 2) return;

            TriggerAlber();
        }
        if (_input.ochsTrigger)
        {
            _input.ochsTrigger = false;
            // if (_currentIndex == 3) return;
        
            TriggerOchs();
        }
        if (_input.ironGateTrigger)
        {
            _input.ironGateTrigger = false;
            // if (_currentIndex == 4) return;
        
            TriggerIronGate();
        }
    }

    public void SwitchGuards()
    {
        //Same as above but with a switch instead
    }
    
    private void OnDestroy()
    {
        if (_graph.IsValid())
        {
            _graph.Destroy();
        }
    }
}