This framework allows you to write custom post-processing effects and plug them to the stack without having to modify the codebase. Of course, all effects written against the framework will work out-of-the-box with volume blending, and unless you need loop-dependent features they'll also automatically work with upcoming  [Scriptable Render Pipelines](https://github.com/Unity-Technologies/ScriptableRenderLoop)!

Let's write a very simple grayscale effect to show it off.

Custom effects need a minimum of two files: a C# and a HLSL source files (note that HLSL gets cross-compiled to GLSL, Metal and others API by Unity so it doesn't mean it's restricted to DirectX).

> **Note:** this quick-start guide requires moderate knowledge of C# and shader programming. We won't go over every detail here, consider it as an overview more than an in-depth tutorial.

## C#

Full code listing:

```csharp
using System;
using UnityEngine;
using UnityEngine.Rendering.PostProcessing;
 
[Serializable]
[PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Custom/Grayscale")]
public sealed class Grayscale : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
}
 
public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
```

> **Important**: this code has to be stored in a file named `Grayscale.cs`. Because of how serialization works in Unity, you have to make sure that the file is named after your settings class name or it won't be serialized properly.

We need two classes, one to store settings (data) and another one to handle the rendering part (logic).

### Settings

The settings class holds the data for our effect. These are all the user-facing fields you'll see in the volume inspector.

```csharp
[Serializable]
[PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Custom/Grayscale")]
public sealed class Grayscale : PostProcessEffectSettings
{
    [Range(0f, 1f), Tooltip("Grayscale effect intensity.")]
    public FloatParameter blend = new FloatParameter { value = 0.5f };
}
```

First, you need to make sure this class extends `PostProcessEffectSettings` and can be serialized, so don't forget the `[Serializable]` attribute!

Second, you'll need to tell Unity that this is a class that holds post-processing data. That's what the `[PostProcess()]` attribute is for. First parameter links the settings to a renderer (more about that in the next section). Second parameter is the injection point for the effect. Right now you have 3 of those available:

- `BeforeTransparent`: the effect will only be applied to opaque objects before the transparent pass is done.
- `BeforeStack`: the effect will be applied before the built-in stack kicks-in. That includes anti-aliasing, depth-of-field, tonemapping etc.
- `AfterStack`: the effect will be applied after the builtin stack and before FXAA (if it's enabled) & final-pass dithering.

The third parameter is the menu entry for the effect. You can use `/` to create sub-menu categories.

Finally, there's an optional fourth parameter `allowInSceneView` which, as its name suggests, enables the effect in the scene view or not. It's set to `true` by default but you may want to disable it for temporal effects or effects that make level editing hard.

For parameters themselves you can use any type you need, but if you want these to be overridable and blendable in volumes you'll have to use boxed fields. In our case we'll simply add a `FloatParameter` with a fixed range going from `0` to `1`. You can get a full list of builtin parameter classes by browsing through the `ParameterOverride.cs` source file in `/PostProcessing/Runtime/`, or you can create your own quite easily by following the way it's done in that same source file.

Note that you can also override the `IsEnabledAndSupported()` method of `PostProcessEffectSettings` to set your own requirements for the effect (in case it requires specific hardware) or even to silently disable the effect until a condition is met. For example, in our case we could automatically disable the effect if the blend parameter is `0` like this:

```csharp
public override bool IsEnabledAndSupported(PostProcessRenderContext context)
{
    return enabled.value
        && blend.value > 0f;
}
```

That way the effect won't be executed at all unless `blend > 0`.

### Renderer

Let's look at the rendering logic now. Our renderer extends `PostProcessEffectRenderer<T>`, with `T` being the settings type to attach to this renderer.

```csharp
public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
{
    public override void Render(PostProcessRenderContext context)
    {
        var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
        sheet.properties.SetFloat("_Blend", settings.blend);
        context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
    }
}
```

Everything happens in the `Render()` method that takes a `PostProcessRenderContext` as parameter. This context holds useful data that you can use and is passed around effects when they are rendered. Look into `/PostProcessing/Runtime/PostProcessRenderContext.cs` for a list of what's available (the file is heavily commented).

`PostProcessEffectRenderer<T>` also have a few other methods you can override, such as:

- `void Init()`: called when the renderer is created.
- `DepthTextureMode GetLegacyCameraFlags()`: used to set camera flags and request depth map, motion vectors, etc.
- `void ResetHistory()`: called when a "reset history" event is dispatched. Mainly used for temporal effects to clear history buffers and whatnot.
- `void Release()`: called when the renderer is destroyed. Do your cleanup there if you need it.

Our effect is quite simple. We need two things:

- Send the `blend` parameter value to the shader.
- Blit a fullscreen pass with the shader to a destination using our source image as an input.

Because we only use command buffers, the system relies on `MaterialPropertyBlock` to store shader data. You don't need to create those yourself as the framework does automatic pooling for you to save time and make sure performances are optimal. So we'll just request a `PropertySheet` for our shader and set the uniform in it.

Finally we use the `CommandBuffer` provided by the context to blit a fullscreen pass with our source, destination, sheet and pass number.

And that's it for the C# part.

## Shader

Writing custom effect shaders is fairly straightforward as well, but there are a few things you should know before you get to it. This framework makes heavy use of macros to abstract platform differences and make your life easier. Compatibility is key, even more so with the upcoming Scriptable Render Pipelines.

Full code listing:

```hlsl
Shader "Hidden/Custom/Grayscale"
{
    HLSLINCLUDE

        #include "PostProcessing/Shaders/StdLib.hlsl"

        TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
        float _Blend;

        float4 Frag(VaryingsDefault i) : SV_Target
        {
            float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
            float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
            color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
            return color;
        }

    ENDHLSL

    SubShader
    {
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            HLSLPROGRAM

                #pragma vertex VertDefault
                #pragma fragment Frag

            ENDHLSL
        }
    }
}
```

First thing to note: we don't use `CG` blocks anymore. If future compatibility with Scriptable Render Pipelines is important to you, do not use them as they'll break the shader when switching over because `CG` blocks add hidden code you don't want to the shader. Instead, use `HLSL` blocks.

At a minimum you'll need to include `StdLib.hlsl`. This holds pre-configured vertex shaders and varying structs (`VertDefault`, `VaryingsDefault`) and most of the data you need to write common effects.

Texture declaration is done using macros. To get a list of available macros we recommend you look into one of the api files in `/PostProcessing/Shaders/API/`.

Other than that, the rest is standard shader code. Here we compute the luminance for the current pixel, we lerp the pixel color with the luminance using the `_Blend` uniform and we return the result.

> **Important:** if the shader is never referenced in any of your scenes it won't get built and the effect will not work when running the game outside of the editor. Either add it to a [Resources folder](https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html) or put it in the **Always Included Shaders** list in `Edit -> Project Settings -> Graphics`.

## Effect ordering

Builtin effects are automatically ordered, but what about custom effects? As soon as you create a new effect or import it into your project it'll be added to the `Custom Effect Sorting` lists in the `Post Process Layer` component on your camera(s).

> **TODO:** editor UI screenshot

They will be pre-sorted by injection point but you can re-order these at will. The order is per-layer, which means you can use different ordering schemes per-camera.

## Custom editor

By default editors for settings classes are automatically created for you. But sometimes you'll want more control over how fields are displayed. Like classic Unity components, you have the ability to create custom editors.

> **Important:** like classic editors, you'll have to put these in an `Editor` folder.

If we were to replicate the default editor for our `Grayscale` effect, it would look like this:

```csharp
using UnityEngine.Rendering.PostProcessing;
using UnityEditor.Rendering.PostProcessing;

[PostProcessEditor(typeof(Grayscale))]
public sealed class GrayscaleEditor : PostProcessEffectEditor<Grayscale>
{
    SerializedParameterOverride m_Blend;

    public override void OnEnable()
    {
        m_Blend = FindParameterOverride(x => x.blend);
    }

    public override void OnInspectorGUI()
    {
        PropertyField(m_Blend);
    }
}
```

## Additional notes

For performance reasons, FXAA expects the LDR luminance value of each pixel to be stored in the alpha channel of its source target. If you need FXAA and wants to inject custom effects at the `AfterStack` injection point, make sure that the last executed effect contains LDR luminance in the alpha channel (or simply copy alpha from the incoming source). If it's not FXAA won't work correctly.