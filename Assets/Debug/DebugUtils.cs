using UnityEngine;

public static class DebugUtils
{
    // check current forward of object?
    public static void DebugForward(Transform obj)
    {
        Debug.Log($"Position of {obj} is {obj.position}");
    }
    

    public static void DebugPos(Transform obj)
    {
        Debug.Log($"Position of {obj} is {obj.position}");
    }
    
    
}
