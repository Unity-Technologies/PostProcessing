using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

namespace UnityEditor.Rendering.PostProcessing
{

public class PostProcessDefaultResources : ScriptableObject
{
    public PostProcessResources resources;

    void OnValidate()
    {
        PostProcessResourceStripper.Update();
    }
}

}
