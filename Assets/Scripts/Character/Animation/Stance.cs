using System;
using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(fileName = "Stance", menuName = "ScriptableObjects/Stances", order = 1)]
public class Stance : ScriptableObject
{
    public string Name;
    public AnimationClipPlayable playable;

    public bool isActive;

    public StanceType type;

    private void OnValidate()
    {
        Name = ConvertTypeToString();
    }

    private string ConvertTypeToString()
    {
        string name = string.Empty;
        
        switch (type)
        {
            case StanceType.VomTag:
                return name = "VomTag";
                
            case StanceType.Pflug:
                return name = "Pflug";
                
            case StanceType.Alber:
                return name = "Alber";
                
            case StanceType.Ochs:
                return name = "Ochs";
                
            case StanceType.Langort:
                return name = "Langort";
                
            case StanceType.IronGate:
                return name = "IronGate";
                
            
        }

        if(name == string.Empty)
        {
            Debug.LogError("Name is not set");
        }

        return string.Empty;
    }
}