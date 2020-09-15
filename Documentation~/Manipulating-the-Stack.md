# Controlling effects using scripts

This guide explains how to modify a post-processing script to create time-based events or temporary post-processing effects.

## Quick Volumes

Use the `QuickVolume` method to quickly spawn new volumes in the scene, to create time-based events or temporary states:

```csharp
[
public PostProcessVolume QuickVolume(int layer, float priority, params PostProcessEffectSettings[] settings) 
]
The following example demonstrates how to use a script to create a pulsating vignette effect:
[
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
public class VignettePulse : MonoBehaviour
{
   PostProcessVolume m_Volume;
   Vignette m_Vignette
   void Start()
  {
      // Create an instance of a vignette
       m_Vignette = ScriptableObject.CreateInstance<Vignette>();
       m_Vignette.enabled.Override(true);
       m_Vignette.intensity.Override(1f);
      // Use the QuickVolume method to create a volume with a priority of 100, and assign the vignette to this volume
       m_Volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, m_Vignette);
   void Update()
  {
       // Change vignette intensity using a sinus curve
        m_Vignette.intensity.value = Mathf.Sin(Time.realtimeSinceStartup);
  }
   void OnDestroy()
  {
       RuntimeUtilities.DestroyVolume(m_Volume, true, true);
  }
}
```

This code creates a new vignette and assigns it to a newly spawned volume. Then, on every frame, it changes the vignette intensity.To avoid memory leaks, destroy the volume and the attached profile when you don’t need them anymore.

## Using tweening libraries with effects

To change the parameters of effect over time or based on a gameplay event, you can manipulate Volume or effect parameters. You can do this in an Update method (as demonstrated in the vignette example above), or you can use a tweening library.

A tweening library is a code library that provides utility functions for simple, code-based animations called "tweens". A few third-party tweening libraries are available for Unity for free, such as[ DOTween](http://dotween.demigiant.com/),[ iTween](http://www.pixelplacement.com/itween/index.php) or[ LeanTween](https://github.com/dentedpixel/LeanTween). The following example uses DOTween. For more information, see the [DOTween documentation](http://dotween.demigiant.com/documentation.php).

This example spawns a volume with a vignette. Its weight is initially set to 0. The code uses the [sequencing feature](http://dotween.demigiant.com/documentation.php#creatingSequence) of DOTween to chain a set of tweening events that set the value of the weight parameter: fade in, pause for a second, fade out. After this sequence has completed, the code destroys the Volume and the `Vignette Pulse` component.

```csharp
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
using DG.Tweening;
public class VignettePulse : MonoBehaviour
{
   void Start()
  {
       var vignette = ScriptableObject.CreateInstance<Vignette>();
       vignette.enabled.Override(true);
       vignette.intensity.Override(1f);
       var volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, vignette);
       volume.weight = 0f;
       DOTween.Sequence()
          .Append(DOTween.To(() => volume.weight, x => volume.weight = x, 1f, 1f))
          .AppendInterval(1f)
          .Append(DOTween.To(() => volume.weight, x => volume.weight = x, 0f, 1f))
          .OnComplete(() =>
          {
               RuntimeUtilities.DestroyVolume(volume, true, true);
               Destroy(this);
          });
  }
}
```

## Profile Editing

The above examples demonstrate how to create new effects and Volumes at runtime, but you can also manually edit an existing Profile that is used by one or more Volumes. To do this, you can use one of two methods which have slightly different effects:

- Modify the shared profile directly:
  - Class field name: `sharedProfile`
  - Applies changes to all volumes using the same profile
  - Modifies the asset and doesn’t reset when you exit play mode
- Request a clone of the shared Profile that will only be used for this Volume:
  - Class field name: `profile`
  - Applies changes to the specified volume
  - Resets when you exit play mode
  - You must manually destroy the profile when you don't need it anymore

The `PostProcessProfile` class contains the following utility methods to help you manage assigned effects: 
| Utility method                                               | **Description**                                              |
| ------------------------------------------------------------ | ------------------------------------------------------------ |
| `T AddSettings()`                                            | Creates, adds and returns a new effect of type `T` to the profile. It throws an exception if it already exist |
| `PostProcessEffectSettings AddSettings(PostProcessEffectSettings effect)` | Adds and returns an effect that you created to the profile.  |
| `void RemoveSettings()`                                      | Removes an effect from the profile. It throws an exception if it doesn't exist. |
| `bool TryGetSettings(out T outSetting)`                      | Gets an effect from the profile, returns `true` if it found a profile, or `false` if it did not find a profile. |

You can find more methods in the `/PostProcessing/Runtime/PostProcessProfile.cs` source file.

**Important:** You must destroy any manually created profiles or effects.

## Additional notes

If you need to instantiate `PostProcessLayer` at runtime, you must bind your resources to it. To do this, add your component and call `Init()` on your `PostProcessLayer` with a reference to the `PostProcessResources` file as a parameter.

Here is an example:

```csharp
var postProcessLayer = gameObject.AddComponent<PostProcessLayer>();
postProcessLayer.Init(resources);
```