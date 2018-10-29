## Quick Volumes

While working on a game you'll often need to push effect overrides on the stack for time-based events or temporary states. You could dynamically create a global volume on the scene, create a profile, create a few overrides, put them into the profile and assign the profile to the volume but that's not very practical.

We provide a `QuickVolume` method to quickly spawn new volumes in the scene:

```csharp
public PostProcessVolume QuickVolume(int layer, float priority, params PostProcessEffectSettings[] settings)
```

First two parameters are self-explanatory. The last parameter takes an array or a list of effects you want to override in this volume.

Instancing a new effect is fairly straightforward. For instance, to create a Vignette effect and override its enabled & intensity fields:

```csharp
var vignette = ScriptableObject.CreateInstance<Vignette>();
vignette.enabled.Override(true);
vignette.intensity.Override(1f);
```

Now let's look at a slightly more complex effect. We want to create a pulsating vignette effect entirely from script:

```csharp
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;

public class VignettePulse : MonoBehaviour
{
    PostProcessVolume m_Volume;
    Vignette m_Vignette;

    void Start()
    {
        m_Vignette = ScriptableObject.CreateInstance<Vignette>();
        m_Vignette.enabled.Override(true);
        m_Vignette.intensity.Override(1f);

        m_Volume = PostProcessManager.instance.QuickVolume(gameObject.layer, 100f, m_Vignette);
    }

    void Update()
    {
        m_Vignette.intensity.value = Mathf.Sin(Time.realtimeSinceStartup);
    }

    void Destroy()
    {
        RuntimeUtilities.DestroyVolume(m_Volume, true);
    }
}
```

This code creates a new vignette and assign it to a newly spawned volume with a priority of `100`. Then, on every frame, it changes the vignette intensity using a sinus curve.

> **Important:** Don't forget to destroy the volume and the attached profile when you don't need them anymore!

## Fading Volumes

Distance-based volume blending is great for most level design use-cases, but once in a while you'll want to trigger a fade in and/or out effect based on a gameplay event. You could do it manually in an `Update` method as described in the previous section or you could use a tweening library to do all the hard work for you. A few of these are available for Unity for free, like [DOTween](http://dotween.demigiant.com/), [iTween](http://www.pixelplacement.com/itween/index.php) or [LeanTween](https://github.com/dentedpixel/LeanTween).

Let's use DOTween for this example. We won't go into details about it (it already has a good [documentation](http://dotween.demigiant.com/documentation.php)) but this should get you started:

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
                RuntimeUtilities.DestroyVolume(volume, true);
                Destroy(this);
            });
    }
}
```

In this example, like the previous one, we spawn a quick volume with a vignette. We set its `weight`property to `0` as we don't want it to have any contribution just yet.

Then we use the sequencing feature of DOTween to chain a set of tweening events: fade in, pause for a second, fade out and finally destroy the volume and the component itself once it's done.

And that's it. Of course you can also tween individual effect properties instead of the volume as a whole, it's up to you.

## Profile Editing

You can also manually edit an existing profile on a volume. It's very similar to how material scripting works in Unity. There are two ways of doing that: either by modifying the shared profile directly or by requesting a clone of the shared profile that will only be used for this volume.

Each method comes with a a few advantages and downsides:

- Shared profile editing:
  - Changes will be applied to all volumes using the same profile
  - Modifies the actual asset and won't be reset when you exit play mode
  - Field name: `sharedProfile`
- Owned profile editing:
  - Changes will only be applied to the specified volume
  - Resets when you exit play mode
  - It is your responsibility to destroy the profile when you don't need it anymore
  - Field name: `profile`

The `PostProcessProfile` class has a few utility methods to help you manage assigned effects. Notable ones include:

- `T AddSettings<T>()`: creates, adds and returns a new effect or type `T` to the profile. Will throw an exception if it already exists.
- `PostProcessEffectSettings AddSettings(PostProcessEffectSettings effect)`: adds and returns an effect you created yourself to the profile.
- `void RemoveSettings<T>()`: removes an effect from the profile. Will throw an exception if it doesn't exist.
- `bool TryGetSettings<T>(out T outSetting)`: gets an effect from the profile, returns `true` if one was found, `false` otherwise.

You'll find more methods by browsing the `/PostProcessing/Runtime/PostProcessProfile.cs` source file.

> **Important:** Don't forget to destroy any manually created profiles or effects.

## Additional notes

If you need to instantiate `PostProcessLayer` at runtime you'll need to make sure resources are properly bound to it. After the component has been added, don't forget to call `Init()` on it with a reference to the `PostProcessResources` file as a parameter.

```csharp
var postProcessLayer = gameObject.AddComponent<PostProcessLayer>();
postProcessLayer.Init(resources);
```