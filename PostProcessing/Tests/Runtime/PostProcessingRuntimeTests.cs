using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using UnityEngine.TestTools;
using UnityEngine.SceneManagement;

#if UNITY_EDITOR
using UnityEditor;
#endif

class PostProcessingTests : IPrebuildSetup
{
    const float k_Epsilon = 1e-4f;

    static List<string> s_Scenes;

    static PostProcessingTests()
    {
        s_Scenes = new List<string>
        {
            "Packages/com.unity.postprocessing/PostProcessing/Tests/Runtime/Scenes/0010_Volumes.unity",
            "Packages/com.unity.postprocessing/PostProcessing/Tests/Runtime/Scenes/0011_Weight.unity",
            "Packages/com.unity.postprocessing/PostProcessing/Tests/Runtime/Scenes/0012_Intersect.unity",
            "Packages/com.unity.postprocessing/PostProcessing/Tests/Runtime/Scenes/0013_Priority.unity",
        };
    }

    public void Setup()
    {
#if UNITY_EDITOR
        Debug.Log("Adding scenes to build settings...");

        var scenes = EditorBuildSettings.scenes.ToList();

        for (int i = 0; i < s_Scenes.Count; i++)
        {
            if (!scenes.Exists(x => x.path == s_Scenes[i]))
                scenes.Add(new EditorBuildSettingsScene(s_Scenes[i], true));
        }

        EditorBuildSettings.scenes = scenes.ToArray();
#endif
    }

    static PostProcessLayer GrabPostProcessLayer()
    {
        var layer = Object.FindObjectOfType<PostProcessLayer>();
        Assert.IsNotNull(layer, "Couldn't find PostProcessLayer");
        return layer;
    }

    [Test]
    public void SettingsManagement()
    {
        var profile = ScriptableObject.CreateInstance<PostProcessProfile>();

        var bloom = profile.AddSettings<Bloom>();
        Assert.IsNotNull(bloom);

        Bloom outBloom;
        var exists = profile.TryGetSettings(out outBloom);
        Assert.IsTrue(exists);
        Assert.IsNotNull(outBloom);

        ChromaticAberration outChroma;
        exists = profile.TryGetSettings(out outChroma);
        Assert.IsFalse(exists);
        Assert.IsNull(outChroma);

        Assert.IsTrue(profile.HasSettings<Bloom>());
        Assert.IsFalse(profile.HasSettings<ChromaticAberration>());

        Assert.IsNotNull(profile.GetSetting<Bloom>());
        Assert.IsNull(profile.GetSetting<ChromaticAberration>());

        profile.RemoveSettings<Bloom>();
        Assert.IsFalse(profile.HasSettings<Bloom>());

        Object.DestroyImmediate(profile);
    }

    [UnityTest]
    public IEnumerator GlobalVolumeSingleLayer()
    {
        SceneManager.LoadScene(s_Scenes[0]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = LayerMask.GetMask("Default");

        yield return null; // Skip a frame

        var vignette = ppLayer.GetSettings<Vignette>();
        Assert.AreEqual(1f, vignette.intensity.value, k_Epsilon);
        Assert.IsTrue(vignette.rounded.value);
    }

    [UnityTest]
    public IEnumerator LocalVolumeMultiLayer()
    {
        SceneManager.LoadScene(s_Scenes[0]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = -1;

        yield return null; // Skip a frame

        var vignette = ppLayer.GetSettings<Vignette>();
        Assert.AreEqual(Color.red, vignette.color.value);
    }

    [UnityTest]
    public IEnumerator LocalVolumeWeight()
    {
        SceneManager.LoadScene(s_Scenes[1]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = -1;

        yield return null; // Skip a frame

        var vignette = ppLayer.GetSettings<Vignette>();
        Assert.AreEqual(0.5f, vignette.color.value.r, k_Epsilon);
    }

    [UnityTest]
    public IEnumerator LocalVolumeIntersect()
    {
        SceneManager.LoadScene(s_Scenes[2]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = -1;

        yield return null; // Skip a frame

        var vignette = ppLayer.GetSettings<Vignette>();
        Assert.AreEqual(0.75f, vignette.color.value.r, k_Epsilon);
    }

    [UnityTest]
    public IEnumerator GetHighestPriority()
    {
        SceneManager.LoadScene(s_Scenes[3]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = -1;

        yield return null; // Skip a frame

        var volume = PostProcessManager.instance.GetHighestPriorityVolume(ppLayer);
        Assert.IsNotNull(volume);
        Assert.AreEqual(10000, volume.priority, k_Epsilon);
    }

    [UnityTest]
    public IEnumerator GetActiveVolumes()
    {
        SceneManager.LoadScene(s_Scenes[3]);

        yield return null; // Skip a frame

        var ppLayer = GrabPostProcessLayer();
        ppLayer.volumeLayer = -1;

        yield return null; // Skip a frame

        var results = new List<PostProcessVolume>();
        PostProcessManager.instance.GetActiveVolumes(ppLayer, results, true, true);
        Assert.AreEqual(3, results.Count, k_Epsilon);
    }
}
