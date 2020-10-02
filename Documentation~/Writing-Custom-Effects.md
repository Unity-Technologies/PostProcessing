# Writing custom effects

This quick-start guide demonstrates how to write a custom [post-processing effect](https://docs.unity3d.com/Manual/PostProcessingOverview.html) and include it in the post-processing stack. This process does not require you to modify the codebase.

Custom post-processing effects require a minimum of two files: 

- A C# source file
- An [HLSL](https://en.wikipedia.org/wiki/High-Level_Shading_Language) source file

Unity cross-compiles HLSL to [GLSL](https://docs.unity3d.com/Manual/30_search.html?q=GLSL), [Metal](https://docs.unity3d.com/Manual/Metal.html), and other APIs. This means it is not restricted to DirectX.

This quick-start guide requires a basic knowledge of programming C# in Unity and HLSL shader programming.

## C# source code

The following example demonstrates how to use a C# script to create a custom grayscale post-processing effect. The script in this example calls APIs from the Post Processing framework and works with volume blending. This example is compatible with versions of Unity from 2018 onwards. It is not compatible with [HDRP](https://docs.unity3d.com/Packages/com.unity.render-pipelines.high-definition@latest?subfolder=/manual/):

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

> **Important**: This file name of this script must match the name of its class for it to serialize correctly. In this case, the script must be stored in a file named `Grayscale.cs`.

### Classes

A custom post-processing effect script requires two classes:

- a data class to store settings
- a logic class to render the effect

### Data class

The data class holds all the settings fields the user sees in the Inspector window for that volume profile. Here is how the data class works in the example script described in [C# source code](https://docs.google.com/document/d/1pVuCcjMJ9HVETWARW29j5TYBd6nh025ayoJDC9UNzy4/edit?ts=5f5a3d2b#heading=h.hzftdvnzv4dh):

```csharp
//The Serializable attribute allows Unity to serialize this class and extend PostProcessEffectSettings.
[Serializable] 
// The [PostProcess()] attribute tells Unity that this class holds post-processing data. The first parameter links the settings to a renderer. The second parameter creates the injection point for the effect. The third parameter is the menu entry for the effect. You can use a forward slash (/) to create sub-menu categories. 
[PostProcess(typeof(GrayscaleRenderer), PostProcessEvent.AfterStack, "Custom/Grayscale")]
public sealed class Grayscale : PostProcessEffectSettings
{
 // You can create boxed fields to override or blend parameters. This example uses a FloatParameter with a fixed range from 0 to 1.
  [Range(0f, 1f), Tooltip("Grayscale effect intensity.")] 
  public FloatParameter blend = new FloatParameter { value = 0.5f };
}
```

#### Notes:

The second parameter of the `[PostProcess()]` attribute is the injection point for the effect. There are three injection points available:

- `BeforeTransparent`: Unity only applies the effect to opaque objects before Unity runs the transparent pass.
- `BeforeStack`: Unity injects the effect before applying the built-in stack. This applies to all post processing effects except [Temporal anti-aliasing (TAA),](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest?subfolder=/manual/Anti-aliasing.html) which Unity applies before any injection points.
- `AfterStack`: Unity applies the effect after the built-in stack and before [FXAA](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest?subfolder=/manual/Anti-aliasing#fast-approximate-anti-aliasing.html) (if it's enabled) and final-pass dithering.

The `[PostProcess()]` attribute has an optional fourth parameter called `allowInSceneView`. You can use this parameter to enable or disable the effect in the scene. It's set to true by default, but you can disable it for temporal effects or effects that get in the way of editing the scene easily.

For a full list of built-in parameter classes, see the `ParameterOverride.cs` source file in **/PostProcessing/Runtime/**.

If you want to set your own requirements for the effect or disable it until a condition is met, you can override the `IsEnabledAndSupported()` method of `PostProcessEffectSettings`. For example, the following script automatically disables an effect if the blend parameter is `0`:

```csharp
public override bool IsEnabledAndSupported(PostProcessRenderContext context)
{
    return enabled.value
        && blend.value > 0f;
}
```

### Logic class

The logic class tells Unity how to render this post-processing effect. Here is how the logic class works in the example script described in [C# source code](https://docs.google.com/document/d/1pVuCcjMJ9HVETWARW29j5TYBd6nh025ayoJDC9UNzy4/edit?ts=5f5a3d2b#heading=h.hzftdvnzv4dh):

```csharp
// This renderer extends PostProcessEffectRenderer<T>, where T is the settings type Unity attaches to this renderer.
public sealed class GrayscaleRenderer : PostProcessEffectRenderer<Grayscale>
{
  // Everything that happens in the Render() method takes a PostProcessRenderContext as parameter.
 public override void Render(PostProcessRenderContext context)
  {
// Request a PropertySheet for our shader and set the uniform within it.
       var sheet = context.propertySheets.Get(Shader.Find("Hidden/Custom/Grayscale"));
// Send the blend parameter value to the shader.       
       sheet.properties.SetFloat("_Blend", settings.blend);
 // This context provides a command buffer which you can use to blit a fullscreen pass using a source image as an input with a destination for the shader, sheet and pass number.
      context.command.BlitFullscreenTriangle(context.source, context.destination, sheet, 0);
  }
}
```

#### Notes:

The `PostProcessRenderContext` context holds data that Unity passes around effects when it renders them. You can find out what data is available in **/PostProcessing/Runtime/PostProcessRenderContext.cs**.

The example grayscale effect script only uses [command buffers](https://docs.unity3d.com/ScriptReference/Rendering.CommandBuffer.html) which means the system relies on `MaterialPropertyBlock` to store shader data. Unity’s framework automatically pools the shader data to save time and optimize performance. This script requests a `PropertySheet` for the shader and sets the uniform inside it, so that you do not need to create `MaterialPropertyBlock` manually.

`PostProcessEffectRenderer<T>` has the following methods you can override:

| Method                                    | Description                                                  |
| ----------------------------------------- | ------------------------------------------------------------ |
| `void Init()`                             | This method is called when the renderer is created           |
| `DepthTextureMode GetLegacyCameraFlags()` | This method sets Camera flags and requests depth maps, motion vectors, etc. |
| `void ResetHistory()`                     | This method is called when a "reset history" event is dispatched. Use this to clear history buffers for temporal effects. |
| `void Release()`                          | This method is called when the renderer is destroyed. You can perform a cleanup here |

## HLSL source code

Unity uses the HLSL programming language for shader programs. For more information, see [Shading language used in Unity](https://docs.unity3d.com/Manual/SL-ShadingLanguage.html).

The following example demonstrates how to create an HLSL script for a custom grayscale post-processing effect: 

```hlsl
Shader "Hidden/Custom/Grayscale"
{
  HLSLINCLUDE 
// StdLib.hlsl holds pre-configured vertex shaders (VertDefault), varying structs (VaryingsDefault), and most of the data you need to write common effects.
      #include "Packages/com.unity.postprocessing/PostProcessing/Shaders/StdLib.hlsl"
      TEXTURE2D_SAMPLER2D(_MainTex, sampler_MainTex);
// Lerp the pixel color with the luminance using the _Blend uniform.      
      float _Blend;
      float4 Frag(VaryingsDefault i) : SV_Target
      {
          float4 color = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, i.texcoord);
// Compute the luminance for the current pixel
          float luminance = dot(color.rgb, float3(0.2126729, 0.7151522, 0.0721750));
          color.rgb = lerp(color.rgb, luminance.xxx, _Blend.xxx);
// Return the result
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

#### Notes: 

This framework uses [shader preprocessor macros](https://docs.unity3d.com/Manual/SL-BuiltinMacros.html) to define platform differences. This helps to maintain compatibility across platforms and render pipelines.

This script also uses macros to declare textures. For a list of available macros, see the API files in **/PostProcessing/Shaders/API/**.

This script uses HLSL blocks instead of CG blocks to avoid compatibility issues between render pipelines. Inside the HLSL block is the varying structs parameter, `VaryingsDefault i`. This parameter can use the following variables:
```hlsl
struct VaryingsDefault
{
   float4 vertex : SV_POSITION;
   float2 texcoord : TEXCOORD0;
   float2 texcoordStereo : TEXCOORD1;
#if STEREO_INSTANCING_ENABLED
   uint stereoTargetEyeIndex : SV_RenderTargetArrayIndex;
#endif
};
```

- `vertex`: The vertex position.
- `texcoord`: The uv coordinate for the fragment.
- `texcoordStereo`: The uv coordinate for the fragment in stereo mode
- `stereoTargetEyeIndex`: The current eye index (only available with VR + stereo instancing).

Unity cannot build a shader if it is not referenced in a scene. This means the effect does not work when the application is running outside of the Editor. To fix this, add your shader to a[ Resources folder](https://docs.unity3d.com/Manual/LoadingResourcesatRuntime.html) or include it in the [**Always Included Shaders**](https://docs.unity3d.com/Manual/class-GraphicsSettings.html#Always) list in **Edit > Project Settings > Graphics**.

## Ordering post-processing effects

Unity automatically sorts built-in effects, but it handles custom effects differently. When you create a new effect or import it into your project, Unity does the following: 

- Adds the custom effect to the Custom Effect Sorting list in the Post Process Layer component on a Camera.
- Sorts each custom effect by its injection point. You can change the order of the custom effects in the [Post Process Layer](https://docs.unity3d.com/Packages/com.unity.postprocessing@2.3/manual/Quick-start.html) component using **Custom Effect Sorting**.

Unity orders custom effects within each layer, which means you can order your custom effects differently for each Camera.

## Creating a custom editor

Unity automatically creates editors for settings classes. If you want to control how Unity displays certain fields, you can create a custom editor.

The following example uses the default editor script to create a custom editor for a `Grayscale` effect:

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

#### Notes: 

For Unity to recognise your custom editor, it has to be in an Editor folder.

## FXAA compatibility with custom effects 

If you apply [**FXAA**](https://docs.unity3d.com/Packages/com.unity.postprocessing@latest?subfolder=/manual/Anti-aliasing.html) in a scene with custom effects, the order of your post-processing effects might stop it from working correctly. This is because FXAA looks for the LDR (Low dynamic range) luminance value of each pixel in the alpha channel of its source target to optimise performance.

If you inject custom effects at the `AfterStack` injection point, as demonstrated in the example above, FXAA looks for LDR luminance in the last executed effect. If it can’t find it, FXAA won’t work correctly.

You can fix this in your custom shader in one of two ways: 

- Make sure that the last executed effect contains LDR luminance in the alpha channel.
- Copy the alpha from the incoming source.



