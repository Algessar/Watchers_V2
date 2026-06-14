using UnityEngine;
using UnityEngine.Animations;

[CreateAssetMenu(fileName = "Stance", menuName = "ScriptableObjects/Stances", order = 1)]
public class Stance : ScriptableObject
{
    public string Name;
    public AnimationClipPlayable playable;

    public StanceType type;

    public string ConvertTypeToString()
    {
        string name = string.Empty;
        
        switch (type)
        {
            case StanceType.VomTag:
                return name = "VomTag";
                
            case StanceType.Pflug:
                return name = "VomTag";
                
            case StanceType.Alber:
                return name = "VomTag";
                
            case StanceType.Ochs:
                return name = "VomTag";
                
            case StanceType.Langort:
                return name = "VomTag";
                
            case StanceType.IronGate:
                return name = "VomTag";
                
            
        }

        if(name == string.Empty)
        {
            Debug.LogError("Name is not set");
        }

        return string.Empty;
    }
}