using System;
using System.IO;
using System.Text;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

public static class CleanAnimClips
{
#if UNITY_EDITOR
    private const string CleanedClipFolderPath = "Assets/Character/Animations/CleanedClips";
    private const string CleanedClipSignaturePrefix = "AnimationSystem_v2_SourceSignature=";
#endif

    public static AnimationClip GetPlayableClip(
        AnimationClip clip,
        string label,
        bool stripScaleCurves,
        bool debugLogs,
        UnityEngine.Object context)
    {
        if (clip == null) return null;

#if UNITY_EDITOR
        if (!stripScaleCurves)
            return clip;

        int scaleCurveCount = CountScaleCurveBindings(clip);

        if (scaleCurveCount == 0)
            return clip;

        string cleanedClipPath = GetCleanedClipPath(clip);
        string sourceSignature = GetSourceClipSignature(clip, scaleCurveCount);

        AnimationClip cachedClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(cleanedClipPath);

        if (cachedClip != null &&
            IsCachedCleanedClipCurrent(cleanedClipPath, cachedClip, sourceSignature))
        {
            if (debugLogs)
            {
                Debug.Log(
                    $"[CleanAnimClips] Clip '{label}' has {scaleCurveCount} source scale curves. Using cached cleaned clip '{cachedClip.name}'.",
                    context);
            }

            return cachedClip;
        }

        if (cachedClip != null)
        {
            AssetDatabase.DeleteAsset(cleanedClipPath);
        }

        EnsureCleanedClipFolderExists();

        AnimationClip cleanedClip = UnityEngine.Object.Instantiate(clip);
        cleanedClip.name = Path.GetFileNameWithoutExtension(cleanedClipPath);

        int removedCurveCount = RemoveScaleCurves(cleanedClip);

        AssetDatabase.CreateAsset(cleanedClip, cleanedClipPath);
        AssetDatabase.SaveAssets();

        StoreCleanedClipSignature(cleanedClipPath, sourceSignature);

        AnimationClip savedClip =
            AssetDatabase.LoadAssetAtPath<AnimationClip>(cleanedClipPath);

        Debug.Log(
            $"[CleanAnimClips] Clip '{label}' cleaned. Removed {removedCurveCount} scale curves.",
            context);

        return savedClip != null
            ? savedClip
            : cleanedClip;
#else
        if (stripScaleCurves && debugLogs)
        {
            Debug.LogWarning(
                $"[CleanAnimClips] Scale stripping for '{label}' only works in the Unity Editor.",
                context);
        }

        return clip;
#endif
    }

#if UNITY_EDITOR

    public static int CountScaleCurveBindings(AnimationClip clip)
    {
        int scaleCurveCount = 0;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (IsScaleCurveBinding(binding))
                scaleCurveCount++;
        }

        return scaleCurveCount;
    }

    public static string GetScaleCurveSamples(AnimationClip clip, int maxSamples)
    {
        if (maxSamples <= 0)
            return string.Empty;

        var builder = new StringBuilder();

        int sampleCount = 0;
        int totalScaleCurveCount = 0;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!IsScaleCurveBinding(binding))
                continue;

            totalScaleCurveCount++;

            if (sampleCount >= maxSamples)
                continue;

            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append(binding.path);
            builder.Append(':');
            builder.Append(binding.propertyName);

            sampleCount++;
        }

        if (totalScaleCurveCount > sampleCount)
        {
            if (builder.Length > 0)
                builder.Append(", ");

            builder.Append("...");
        }

        return builder.ToString();
    }

    private static int RemoveScaleCurves(AnimationClip clip)
    {
        int removedCurveCount = 0;

        foreach (EditorCurveBinding binding in AnimationUtility.GetCurveBindings(clip))
        {
            if (!IsScaleCurveBinding(binding))
                continue;

            AnimationUtility.SetEditorCurve(clip, binding, null);
            removedCurveCount++;
        }

        return removedCurveCount;
    }

    private static bool IsScaleCurveBinding(EditorCurveBinding binding)
    {
        return binding.propertyName.IndexOf(
            "scale",
            StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static string GetCleanedClipPath(AnimationClip clip)
    {
        string cleanedClipName =
            CleanAssetFileName($"{clip.name.Replace('_', '-')}-ScaleCurvesStripped");

        return $"{CleanedClipFolderPath}/{cleanedClipName}.anim";
    }

    private static string CleanAssetFileName(string fileName)
    {
        foreach (char invalidCharacter in Path.GetInvalidFileNameChars())
        {
            fileName = fileName.Replace(invalidCharacter, '-');
        }

        return fileName;
    }

    private static string GetSourceClipSignature(
        AnimationClip clip,
        int scaleCurveCount)
    {
        string sourcePath = AssetDatabase.GetAssetPath(clip);

        string guid = AssetDatabase.AssetPathToGUID(sourcePath);

        AssetDatabase.TryGetGUIDAndLocalFileIdentifier(
            clip,
            out string localGuid,
            out long localId);

        Hash128 dependencyHash =
            string.IsNullOrEmpty(sourcePath)
                ? default
                : AssetDatabase.GetAssetDependencyHash(sourcePath);

        return $"{CleanedClipSignaturePrefix}{guid}:{localGuid}:{localId}:{dependencyHash}:{clip.length:F6}:{clip.frameRate:F3}:{scaleCurveCount}";
    }

    private static bool IsCachedCleanedClipCurrent(
        string cleanedClipPath,
        AnimationClip cachedClip,
        string sourceSignature)
    {
        if (CountScaleCurveBindings(cachedClip) > 0)
            return false;

        AssetImporter importer = AssetImporter.GetAtPath(cleanedClipPath);

        return importer != null &&
               importer.userData == sourceSignature;
    }

    private static void StoreCleanedClipSignature(
        string cleanedClipPath,
        string sourceSignature)
    {
        AssetImporter importer =
            AssetImporter.GetAtPath(cleanedClipPath);

        if (importer == null)
            return;

        importer.userData = sourceSignature;
        importer.SaveAndReimport();
    }

    private static void EnsureCleanedClipFolderExists()
    {
        if (AssetDatabase.IsValidFolder(CleanedClipFolderPath))
            return;

        AssetDatabase.CreateFolder(
            "Assets/Character/Animations",
            "CleanedClips");
    }

#endif
}