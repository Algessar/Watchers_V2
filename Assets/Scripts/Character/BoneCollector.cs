

using System.Collections.Generic;
using UnityEngine;

public class BoneCollector : MonoBehaviour
{
    [SerializeField] private SkinnedMeshRenderer _skinnedMeshRenderer;

    public List<Transform> bones = new();

    private void Awake()
    {
        bones.Clear();
        bones.AddRange(_skinnedMeshRenderer.bones);

        // foreach (var bone in _bones)
        // {
        //     Debug.Log($"{bone.name} has scale {bone.localScale}");
        // }
    }
}