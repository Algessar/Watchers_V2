using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Animations;
using UnityEngine.Playables;

public class AnimationSystem_v1 : MonoBehaviour
{
    [SerializeField] private Animator animator;
    [SerializeField] private AvatarMask upperBodyMask;   // for guards (layer 1)

    private PlayableGraph graph;
    private AnimationLayerMixerPlayable layerMixer;
    private List<AnimationMixerPlayable> layerMixers = new List<AnimationMixerPlayable>();
    
    // For each layer: clip ID -> mixer input index, and reverse mapping
    private List<Dictionary<int, int>> layerClipIdToIndex = new List<Dictionary<int, int>>();
    private List<Dictionary<int, int>> layerIndexToClipId = new List<Dictionary<int, int>>();
    
    private int nextClipId = 1;
    
    private int[] currentClipPerLayer;


    private class CrossfadeState
    {
        public int layerIndex;
        public int fromId;
        public int toId;
        public float duration;
        public float elapsed;
        public bool active;
    }
    private List<CrossfadeState> activeCrossfades = new List<CrossfadeState>();

    private void Start()
    {
        currentClipPerLayer = new int[2] { -1, -1 };

        
        if (animator == null)
            animator = GetComponentInChildren<Animator>();

        animator.applyRootMotion = false;

        graph = PlayableGraph.Create("AnimationSystem");
        var output = AnimationPlayableOutput.Create(graph, "Output", animator);

        // Create layer mixer with 2 layers (0 = full body, 1 = upper body)
        layerMixer = AnimationLayerMixerPlayable.Create(graph, 2);
        output.SetSourcePlayable(layerMixer);

        // Configure masks
        layerMixer.SetLayerMaskFromAvatarMask(1, upperBodyMask);
        layerMixer.SetInputWeight(0, 1f);
        layerMixer.SetInputWeight(1, 1f);

        // Create a mixer for each layer
        for (int i = 0; i < 2; i++)
        {
            var mixer = AnimationMixerPlayable.Create(graph, 0); // starts with 0 inputs
            graph.Connect(mixer, 0, layerMixer, i);
            layerMixers.Add(mixer);
            layerClipIdToIndex.Add(new Dictionary<int, int>());
            layerIndexToClipId.Add(new Dictionary<int, int>());
        }

        graph.Play();
    }

    /// <summary>
    /// Register an animation clip on a specific layer.
    /// Returns a unique ID that can be used for crossfading.
    /// </summary>
    public int RegisterClip(AnimationClip clip, int layerIndex)
    {
        if (layerIndex < 0 || layerIndex >= layerMixers.Count)
            throw new System.ArgumentException($"Invalid layer index {layerIndex}");

        var mixer = layerMixers[layerIndex];
        int inputIndex = mixer.GetInputCount();
        
        // 1. Increase the mixer's input capacity
        mixer.SetInputCount(inputIndex + 1);
        
        // 2. Create the clip playable
        var clipPlayable = AnimationClipPlayable.Create(graph, clip);
        
        // 3. Connect to the new input slot
        graph.Connect(clipPlayable, 0, mixer, inputIndex);
        
        // 4. Initially weight = 0 (silent)
        mixer.SetInputWeight(inputIndex, 0f);

        int clipId = nextClipId++;
        layerClipIdToIndex[layerIndex][clipId] = inputIndex;
        layerIndexToClipId[layerIndex][inputIndex] = clipId;
        
        return clipId;
    }

    /// <summary>
    /// Directly set the weight of a registered clip (0 = no influence, 1 = full).
    /// </summary>
    public void SetClipWeight(int clipId, float weight)
    {
        for (int layerIdx = 0; layerIdx < layerMixers.Count; layerIdx++)
        {
            if (layerClipIdToIndex[layerIdx].TryGetValue(clipId, out int inputIndex))
            {
                layerMixers[layerIdx].SetInputWeight(inputIndex, weight);
                
                return;
            }
            
        }
        Debug.LogError($"Clip ID {clipId} not found");
    }

    /// <summary>
    /// Crossfade from the currently playing clip on the same layer to a new clip.
    /// </summary>
    public void CrossfadeTo(int clipId, float duration)
    {
        for (int layerIdx = 0; layerIdx < layerMixers.Count; layerIdx++)
        {
            if (layerClipIdToIndex[layerIdx].ContainsKey(clipId))
            {
                int fromId = currentClipPerLayer[layerIdx];
                if (fromId == clipId) return; // already playing
            
                // Cancel any ongoing crossfade on this layer
                activeCrossfades.RemoveAll(cs => cs.layerIndex == layerIdx);
            
                // If no clip is currently playing (fromId == -1), set weight immediately
                if (fromId == -1)
                {
                    SetClipWeight(clipId, 1f);
                    currentClipPerLayer[layerIdx] = clipId;
                    return;
                }
            
                var cs = new CrossfadeState
                {
                    layerIndex = layerIdx,
                    fromId = fromId,
                    toId = clipId,
                    duration = duration,
                    elapsed = 0f,
                    active = true
                };
                activeCrossfades.Add(cs);
                return;
            }
        }
        Debug.LogError($"Clip ID {clipId} not registered");
    }

    private int GetCurrentClipIdOnLayer(int layerIdx)
    {
        var mixer = layerMixers[layerIdx];
        int inputCount = mixer.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            if (mixer.GetInputWeight(i) > 0.5f)
            {
                if (layerIndexToClipId[layerIdx].TryGetValue(i, out int clipId))
                    return clipId;
            }
        }
        return -1; // no active clip
    }

    private void Update()
    {
        // Process all active crossfades
        for (int i = activeCrossfades.Count - 1; i >= 0; i--)
        {
            var cs = activeCrossfades[i];
            if (!cs.active) continue;

            cs.elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(cs.elapsed / cs.duration);

            var mixer = layerMixers[cs.layerIndex];
            int fromIdx = layerClipIdToIndex[cs.layerIndex][cs.fromId];
            int toIdx   = layerClipIdToIndex[cs.layerIndex][cs.toId];
            
            mixer.SetInputWeight(fromIdx, 1 - t);
            mixer.SetInputWeight(toIdx,   t);

            if (t >= 1f)
            {
                // Ensure clean final weights
                mixer.SetInputWeight(fromIdx, 0f);
                mixer.SetInputWeight(toIdx,   1f);
                currentClipPerLayer[cs.layerIndex] = cs.toId;
                activeCrossfades.RemoveAt(i);
            }
        }
        
        DebugPrintWeights(0);
    }
    
    void DebugPrintWeights(int layerIdx)
    {
        var mixer = layerMixers[layerIdx];
        int inputCount = mixer.GetInputCount();
        for (int i = 0; i < inputCount; i++)
        {
            if (layerIndexToClipId[layerIdx].TryGetValue(i, out int clipId))
            {
                float w = mixer.GetInputWeight(i);
                if (w > 0.01f)
                    Debug.Log($"Layer {layerIdx} Clip {clipId} weight = {w}");
            }
        }
    }

    private void OnDestroy()
    {
        if (graph.IsValid())
            graph.Destroy();
    }
}