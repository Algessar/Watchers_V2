#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEngine;

public class StripScaleCurvesOnImport : AssetPostprocessor
{
    void OnPreprocessAnimation()
    {
        // Optional: restrict to a specific folder
        // if (!assetPath.Contains("Character/Animations")) return;
    }

    void OnPostprocessAnimation(GameObject root, AnimationClip clip)
    {
        bool removedAny = false;
        var bindings = AnimationUtility.GetCurveBindings(clip);
        foreach (var binding in bindings)
        {
            if (binding.propertyName.Contains("m_LocalScale") ||
                binding.propertyName.IndexOf("scale", StringComparison.OrdinalIgnoreCase) >= 0)
            {
                AnimationUtility.SetEditorCurve(clip, binding, null);
                removedAny = true;
            }
        }
        if (removedAny)
            Debug.Log($"Stripped scale curves from {clip.name}", clip);
    }
}
#endif