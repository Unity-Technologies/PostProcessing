## Post Processing

**Post-processing** is the process of applying full-screen filters and effects to a camera’s image buffer before it is displayed to screen. It can drastically improve the visuals of your product with little setup time.

You can use post-processing effects to simulate physical camera and film properties.

# Installation

## Package

The standard way to install post-processing or update it to the latest version is to use the package manager that comes with Unity 2018.1.

> **Note:** if you already installed one of the scriptable render pipelines in your project then the post-processing package will already be installed.

Go to `Window -> Package Manager`, switch the view from `In Project` to `All` and select `Postprocessing` in the list. In the right panel you'll find information about the package and a button to install or update to the latest available version for the currently running version of Unity.

## Sources

You can, if you prefer, use the bleeding edge version of post-processing but be aware that only packaged versions are officially supported and things may break. If you're not familiar with Git we recommend you download [Github Desktop](https://desktop.github.com/) as it's easy to use and integrates well with Github.

First, make sure you don't already have the `Postprocessing` package installed or it will conflict with a source installation. If you do, you can remove it using the package manager (`Window -> Package Manager`).

Then you can use your Git client to clone the [post-processing repository](https://github.com/Unity-Technologies/PostProcessing) into your `Assets` folder. The development branch is `v2` and is conveniently set as the default so you don't need to pull any specific branch unless you want to follow a specific feature being developed in a separate branch.

If you don't want to use a Git client you can also download a zip archive by clicking the green button that says "Clone or download" at the top of the repository and extract it into your project. The benefit of using Git is that you can quickly update to the latest revision without having to download / extract / replace the whole thing again. It's also more error-proof as it will handle moving & removing files correctly.

# Quick-Start 

> **Note:** if you created a project using a template that includes the Post-processing package you don't need to go through these steps. First time users should still read through this section for important information about first-time set up and Volumes.

To enable post-processing on a camera, add the `Component -> Rendering -> Post-process Layer` component to it.

## Volume blending

See [Post-process Volumes](## Post-process Volumes) for further information

- **Trigger:** This is the Transform that will be drive the volume blending feature This Transform acts as a trigger for the Volume blending feature. Unity automatically assigns the Camera to this field when a Post-process Layer Component is created. You can use any GameObject for the Trigger, e.g. for a top down game you'll want the player character to drive the blending instead of the Camera Transform.
- **Layer:** The Layer Masks to consider for volume blending. The Camera will only be affected by Volumes assigned to the selected layers. <!--- It allows you to do volume filtering and is especially useful to optimize volume traversal. ---> Assign Post-processing volumes to their own Layers instead of the default Layer for best performance. By default the Layer is set to `Nothing` so don't forget to change it or local volumes won't have any effect.

## Anti-aliasing

Anti-aliasing settings must be set up on each individual camera in your scene. 

Next comes **Anti-aliasing** which has to be setup per-camera. The benefit of doing it that way instead of having a global setting in the project is that you can optimize your cameras to only use anti-aliasing when needed. For instance, your main camera could be using **Temporal Anti-aliasing** but a secondary one used to render a security camera would only require **FXAA**. More information about anti-aliasing is available on the [dedicated effect page](https://github.com/Unity-Technologies/PostProcessing/wiki/Anti-aliasing).

The **Stop NaN Propagation** toggle will kill any invalid / NaN pixel and replace it with a black color before post-processing is applied. It's generally a good idea to keep this enabled to avoid post-processing artifacts cause by broken data in the scene.

The **Toolkit** section comes with a few utilities. You can export the current frame to EXR using one of the following modes:

- **Full Frame (as displayed):** exports the camera as-is (if it's on the camera shown in the Game View, the export will look exactly like what's shown in the Game View).
- **Disable post-processing:** same as the previous mode but without any sort of post-processing applied.
- **Break before Color Grading (linear):** same as the first mode but will stop rendering just before **Color Grading** is applied. This is useful if you want to author grading LUTs in an external software.
- **Break before Color Grading (log):** same as the previous mode but the output will be log-encoded. This is used to author full-precision HDR grading LUTs in an external software.

Other utilities include:

- **Select all layer volumes:** selects all **Post-process Volume** components that can affect this **Post-process Layer**.
- **Select all active volumes:** selects all **Post-process Volume** components currently affecting this **Post-process Layer**.

Finally, the last section allows you to change the rendering order of custom effects. More information on [Writing Custom Effects](https://github.com/Unity-Technologies/PostProcessing/wiki/Writing-Custom-Effects).

## Post-process Volumes

The way post-processing works in this framework is by using local & global volumes. It allows you to give each volume a priority and a set of effect overrides to automatically blend post-processing settings in your scene. For instance, you could have a light vignette effect set-up globally but when the player enters a cave you would only override the `Intensity` setting of the vignette to make it stronger while keeping the rest of the settings intact.

The **Post-process Volume** component can be added to any game object, the camera itself included. But it's generally a good idea to create a dedicated object for each volume. Let's start by creating a global **Post-process Volume**. Create an empty game object and add the component to it (`Component -> Rendering -> Pöst-process Volume`) or use `GameObject -> 3D Object -> Post-process Volume`. Don't forget to add it to a layer that's being used by the mask set in the **Post-process Layer** component you added to your camera.

By default it's completely empty. Volumes come with two modes:

- **Global:** a global volume doesn't have any boundary and will be applied to the whole scene. You can of course have several of these in your scene.
- **Local:** a local volume needs a collider or trigger component attached to it to define its boundaries. Any type of 3D collider will work, from cubes to complex convex meshes but we recommend you use simple colliders as much as possible, as meshes can be quite expensive to traverse. Local volumes can also have a `Blend Distance` that represents the outer distance from the volume surface where blending will start.

In this case we want a global volume so let's enable `Is Global`.

`Weight` can be used to reduce the global contribution of the volume and all its overrides, with 0 being no contribution at all and 1 full contribution.

The `Priority` field defines the volume order in the stack. The higher this number is, the higher priority a volume has.

We also need a to create a profile for this volume (or re-use an existing one). Let's create one by clicking the `New` button (you can also use `Create -> Post-processing Profile` in your project window). It will be created as an asset file in your project. To edit a profile content you can either select this asset or go to a volume inspector where it will be replicated for easier access. Once a profile as been assigned you'll see a new button appear, `Clone`. This one will duplicate the currently assigned profile and set it on the volume automatically. This can be handy when you want to create quick variations of a same profile (although you should really use the override system if possible).

We can now start adding effect overrides to the stack.

The anatomy of an effect is as follow:

Each field has an override checkbox on its left, you'll need to toggle the settings you want to override for this volume before you can edit them. You can quickly toggle them all on or off by using the small `All` and `None` shortcuts at the top left.

The top-right `On/Off` toggle is used to override the active state of the effect itself in the stack (if you want, for instance, to force-disable an effect in a higher priority volume) whereas the toggle in the title bar is used to disable the set of overrides for this effect in this particular volume.

Finally, you can right-click and effect title to show a quick-action menu to copy/paste/remove/reset settings.

# Effects

## Ambient Occlusion

The **Ambient Occlusion** post-processing effect approximates [Ambient Occlusion](http://en.wikipedia.org/wiki/Ambient_occlusion) in real time as a full-screen post-processing effect. It darkens creases, holes, intersections and surfaces that are close to each other. In real life, such areas tend to block out or occlude ambient light, hence they appear darker.

Note that the **Ambient Occlusion** effect is quite expensive in terms of processing time and generally should only be used on desktop or console hardware. Its cost depends purely on screen resolution and the effects parameters and does not depend on scene complexity as true ambient occlusion would.

> **TODO:** before/after screenshot

The effect comes with two modes:

- Scalable Ambient Obscurance
- Multi-scale Volumetric Occlusion

## Scalable Ambient Obscurance

This is a standard implementation of ambient obscurance that works on non modern platforms. If you target a compute-enabled platform we recommend that you use the **Multi-scale Volumetric Occlusion** mode instead.

> **TODO:** editor UI screenshot

### Properties

| Property     | Function                                                     |
| :------------ | :------------------------------------------------------------ |
| Intensity    | Degree of darkness produced by the effect.                   |
| Radius       | Radius of sample points, which affects extent of darkened areas. |
| Quality      | Defines the number of sample points, which affects quality and performance. |
| Color        | Tint of the ambient occlusion.                               |
| Ambient Only | Enables the ambient-only mode in that the effect only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. |

### Performances

Beware that this effect can be quite expensive, especially when viewed very close to the camera. For that reason it is recommended to favor a low `Radius` setting. With a low `Radius` the ambient occlusion effect will only sample pixels that are close, in clip space, to the source pixel, which is good for performance as they can be cached efficiently. With higher radiuses, the generated samples will be further away from the source pixel and won’t benefit from caching thus slowing down the effect. Because of the camera’s perspective, objects near the front plane will use larger radiuses than those far away, so computing the ambient occlusion pass for an object close to the camera will be slower than for an object further away that only occupies a few pixels on screen.

Dropping the `Quality` setting down will improve performances too.

Generally speaking, this effect should not be considered on mobile platforms and when running on consoles we recommend using the **Multi-scale Volumetric Occlusion** mode as it's much faster there and looks better in most cases.

### Requirements

- Depth & Normals textures
- Shader model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Multi-scale Volumetric Occlusion

This is a more modern version of ambient occlusion heavily optimized for consoles and desktop platforms. It generally looks better and runs faster than the other mode on these platforms but requires compute shader support.

> **TODO:** editor UI screenshot

### Properties

| Property           | Function                                                     |
| :------------------ | :------------------------------------------------------------ |
| Intensity          | Degree of darkness produced by the effect.                   |
| Thickness Modifier | Modifies the thickness of occluders. This increases dark areas but can potentially introduces dark halos around objects. |
| Color              | Tint of the ambient occlusion.                               |
| Ambient Only       | Enables the ambient-only mode in that the effect only affects ambient lighting. This mode is only available with the Deferred rendering path and HDR rendering. |

### Requirements

- Compute shader support
- Shader model 4.5

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Anti-aliasing

The **Anti-aliasing** effect offers a set of algorithms designed to prevent aliasing and give a smoother appearance to graphics. Aliasing is an effect where lines appear jagged or have a “staircase” appearance (as displayed in the left-hand image below). This can happen if the graphics output device does not have a high enough resolution to display a straight line.

**Anti-aliasing** reduces the prominence of these jagged lines by surrounding them with intermediate shades of color. Although this reduces the jagged appearance of the lines, it also makes them blurrier.

> **TODO:** before/after screenshot

The Anti-aliasing algorithms are image-based. This is very useful when traditional multisampling (as used in the Editor’s [Quality settings](https://docs.unity3d.com/Manual/class-QualitySettings.html)) is not properly supported or when working with specular-heavy PBR materials.

The algorithms supplied in the post-processing stack are:

- Fast Approximate Anti-aliasing (FXAA)
- Subpixel Morphological Anti-aliasing (SMAA)
- Temporal Anti-aliasing (TAA)

They are set per-camera on the **Post-process Layer** component.

## Fast Approximate Anti-aliasing

**FXAA** is the cheapest technique and is recommended for mobile and other platforms that don’t support motion vectors, which are required for **TAA**.

> **TODO:** editor UI screenshot

### Properties

| Property   | Function                                                     |
| :--------- | :----------------------------------------------------------- |
| Fast Mode  | A slightly lower quality but faster variant of FXAA. Highly recommended on mobile platforms. |
| Keep Alpha | Toggle this on if you need to keep the alpha channel untouched by post-processing. Else it will use this channel to store internal data used to speed up and improve visual quality. |

### Performances

`Fast Mode` should be enabled on mobile & Nintendo Switch as it gives a substantial performance boost compared to the regular mode on these platforms. PS4 and Xbox One slightly benefit from it as well but on desktop GPUs it makes no difference and the regular mode should be used for added visual quality.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Subpixel Morphological Anti-aliasing

**SMAA** is a higher quality anti-aliasing effect than **FXAA** but it's also slower. Depending on the art-style of your game it can work as well as **TAA** while avoiding some of the shortcomings of this technique.

> **TODO:** editor UI screenshot

### Properties

| Property | Function                                         |
| :-------- | :------------------------------------------------ |
| Quality  | The overall quality of the anti-aliasing filter. |

### Performances

Lowering the `Quality` setting will make the effect run faster. Using **SMAA** on mobile platforms isn't recommended.

### Known issues and limitations

- SMAA doesn't support AR/VR.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Temporal Anti-aliasing

**TAA** is a more advanced anti-aliasing technique where frames are accumulated over time in a history buffer to be used to smooth edges more effectively. It is substantially better at smoothing edges in motion but requires motion vectors and is more expensive than **FXAA**. Due to this it is recommended for desktop and console platforms.

> **TODO:** editor UI screenshot

### Properties

| Property            | Function                                                     |
| :------------------- | :------------------------------------------------------------ |
| Jitter Spread       | The diameter (in texels) inside which jitter samples are spread. Smaller values result in crisper but more aliased output, whilst larger values result in more stable but blurrier output. |
| Stationary Blending | The blend coefficient for stationary fragments. Controls the percentage of history sample blended into final color for fragments with minimal active motion. |
| Motion Blending     | The blending coefficient for moving fragments. Controls the percentage of history sample blended into the final color for fragments with significant active motion. |
| Sharpness           | TAA can induce a slight loss of details in high frequency regions. Sharpening alleviates this issue. |

### Known issues and limitations

- Not supported on GLES2 platforms.

### Requirements

- Motion vectors
- Depth texture
- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Auto-Exposure

In ocular physiology, adaptation is the ability of the eye to adjust to various levels of darkness and light. The human eye can function from very dark to very bright levels of light. However, in any given moment of time, the eye can only sense a contrast ratio of roughly one millionth of the total range. What enables the wider reach is that the eye adapts its definition of what is black.

This effect dynamically adjusts the exposure of the image according to the range of brightness levels it contains. The adjustment takes place gradually over a period of time, so the player can be briefly dazzled by bright outdoor light when, say, emerging from a dark tunnel. Equally, when moving from a bright scene to a dark one, the “eye” takes some time to adjust.

Internally, this effect generates a histogram on every frame and filters it to find the average luminance value. This histogram, and as such the effect, requires [Compute shader](https://docs.unity3d.com/Manual/ComputeShaders.html) support.

### Properties

**Exposure** settings:

| Property              | Function                                                     |
| :--------------------- | :------------------------------------------------------------ |
| Filtering             | These values are the lower and upper percentages of the histogram that will be used to find a stable average luminance. Values outside of this range will be discarded and wont contribute to the average luminance. |
| Minimum               | Minimum average luminance to consider for auto exposure (in EV). |
| Maximum               | Maximum average luminance to consider for auto exposure (in EV). |
| Exposure Compensation | Middle-grey value. Use this to compensate the global exposure of the scene. |

**Adaptation** settings:

| Property   | Function                                                     |
| :---------- | :------------------------------------------------------------ |
| Type       | Use Progressive if you want the auto exposure to be animated. Use Fixed otherwise. |
| Speed Up   | Adaptation speed from a dark to a light environment.         |
| Speed Down | Adaptation speed from a light to a dark environment.         |

### Details

Use the `Filtering` range to exclude the darkest and brightest part of the image. To compute an average luminance you generally don’t want very dark and very bright pixels to contribute too much to the result. Values are in percent.

`Minimum`/`Maximum` values clamp the computed average luminance into a given range.

You can also set the `Type` to `Fixed` if you don’t need the eye adaptation effect and it will behave like an auto-exposure setting.

It is recommended to use the **Light Meter** [monitor](https://github.com/Unity-Technologies/PostProcessing/wiki/Debugging) when setting up this effect.

### Requirements

- Compute shader
- Shader model 5

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Bloom

**Bloom** is an effect used to reproduce an imaging artifact of real-world cameras. The effect produces fringes of light extending from the borders of bright areas in an image, contributing to the illusion of an extremely bright light overwhelming the camera or eye capturing the scene.

**Lens Dirt** applies a fullscreen layer of smudges or dust to diffract the Bloom effect. This is commonly used in modern first person shooters.

### Properties

**Bloom** settings:

| Property         | Function                                                     |
| :---------------- | :------------------------------------------------------------ |
| Intensity        | Strength of the Bloom filter.                                |
| Threshold        | Filters out pixels under this level of brightness. This value is expressed in gamma-space. |
| Soft Knee        | Makes transition between under/over-threshold gradual (0 = hard threshold, 1 = soft threshold). |
| Diffusion        | Changes extent of veiling effects in a screen resolution-independent fashion. |
| Anamorphic Ratio | Emulates the effect of an anamorphic lens by scaling the bloom vertically (in range [-1,0]) or horizontally (in range [0,1]). |
| Color            | Tint of the Bloom filter.                                    |
| Fast Mode        | Boost performances by lowering the effect quality.           |

**Dirtiness** settings:

| Property  | Function                                              |
| --------- | ----------------------------------------------------- |
| Texture   | Dirtiness texture to add smudges or dust to the lens. |
| Intensity | Amount of lens dirtiness.                             |

### Details

With properly exposed HDR scenes, `Threshold` should be set to ~1 so that only pixels with values above 1 leak into surrounding objects. You’ll probably want to drop this value when working in LDR or the effect won’t be visible.

### Performances

Lowering the `Diffusion` parameter will make the effect faster. The further away `Anamorphic Ratio` is from 0, the slower it will be. On mobile and low-end platforms it is recommended to enable `Fast Mode` as it gives a significant boost in performances.

Finally, smaller lens dirt textures will result is faster lookup and blending across volumes.

### Requirements

- Shader model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Chromatic Aberration

In photography, chromatic aberration is an effect resulting from a camera’s lens failing to converge all colors to the same point. It appears as “fringes” of color along boundaries that separate dark and bright parts of the image.

The **Chromatic Aberration** effect is used to replicate this camera defect, it is also often used to artistic effect such as part of camera impact or intoxication effects. This implementation provides support for red/blue and green/purple fringing as well as user defined color fringing via an input texture.

### Properties

| Property     | Function                                                     |
| :------------ | :------------------------------------------------------------ |
| Spectral Lut | Texture used for custom fringing color (will use default when empty). |
| Intensity    | Strength of chromatic aberrations.                           |
| Fast Mode    | Use a faster variant of the effect for improved performances. |

### Details

**Chromatic Aberration** uses a `Spectral Lut` input for custom fringing. Four example spectral textures are provided in the repository:

- Red/Blue (Default)
- Blue/Red
- Green/Purple
- Purple/Green

You can create custom spectral textures in any image editing software. Their resolution is not constrained but it is recommended that they are as small as possible (such as the 3x1 textures provided).

You can achieve a less smooth effect by manually setting the `Filter Mode` of the input texture to `Point (no filter)`.

### Performances

Performances depend on the `Intensity` value (the higher it is, the slower the render will be as it will need more samples to render smooth chromatic aberrations).

Enabling `Fast Mode` is also recommended whenever possible as it's a lot faster, albeit not as smooth as the regular mode.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Color Grading

Color grading is the process of altering or correcting the color and luminance of the final image. You can think of it like applying filters in software like Instagram.

The **Color Grading** effect comes with three modes:

- **Low Definition Range:** this mode is aimed at lower-end platforms but it can be used on any platform. Grading is applied to the final rendered frame clamped in a [0,1] range and stored in a standard LUT.
- **High Definition Range:** this mode is aimed at platforms that support HDR rendering. All the color operations will be applied in HDR and stored into a 3D log-encoded LUT to ensure a sufficient range coverage and precision (Alexa LogC El1000).
- **External:** this mode allows you to provide a custom 3D LUT authored in an external software. **TODO:** tutorial

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

### Global settings

> **Note:** these are only available for the **Low Definition Range** and **External** modes.

#### Properties

| Property       | Function                                                     |
| :-------------- | :------------------------------------------------------------ |
| Lookup Texture | **LDR:** A custom lookup texture (strip format, e.g. 256x16) to apply before the rest of the color grading operators. If none is provided, a neutral one will be generated internally.<br />**External**: A custom 3D log-encoded texture. **TODO:** tutorial |

### Tonemapping

Tonemapping is the process of remapping HDR values of an image into a range suitable to be displayed on screen. Tonemapping should always be applied when using an HDR camera, otherwise values color intensities above 1 will be clamped at 1, altering the scenes luminance balance.

The **High Definition Range** mode comes with 4 tonemapping operators:

- **None:** no tonemapping will be applied.
- **Neutral:** only does range-remapping with minimal impact on color hue & saturation and is generally a great starting point for extensive color grading.
- **ACES**: uses a close approximation of the reference [ACES](http://www.oscars.org/science-technology/sci-tech-projects/aces) tonemapper for a more filmic look. Because of that, it is more contrasted than **Neutral** and has an effect on actual color hue & saturation. Note that if you enable this tonemapper all the grading operations will be done in the ACES color spaces for optimal precision and results.
- **Custom:** a fully parametric tonemapper.

> **Note**: these are only available for the **High Definition Range** mode.

#### Properties

> **Note**: **Custom** is the only tonemapper with settings.

| Property          | Function                                                     |
| :----------------- | :------------------------------------------------------------ |
| Toe Strength      | Affects the transition between the toe and the mid section of the curve. A value of 0 means no toe, a value of 1 means a very hard transition. |
| Toe Length        | Affects how much of the dynamic range is in the toe. With a small value, the toe will be very short and quickly transition into the linear section, and with a longer value having a longer toe. |
| Shoulder Strength | Affects the transition between the mid section and the shoulder of the curve. A value of 0 means no shoulder, value of 1 means a very hard transition. |
| Shoulder Length   | Affects how many F-stops (EV) to add to the dynamic range of the curve. |
| Shoulder Angle    | Affects how much overshot to add to the shoulder.            |
| Gamma             | Applies a gamma function to the curve.                       |

### White Balance


#### Properties

| Property    | Function                                                     |
| :----------- | :------------------------------------------------------------ |
| Temperature | Sets the white balance to a custom color temperature.        |
| Tint        | Sets the white balance to compensate for a green or magenta tint. |

### Tone


#### Properties

| Property      | Function                                                     |
| :------------- | :------------------------------------------------------------ |
| Post-exposure | Adjusts the overall exposure of the scene in EV units. This is applied after HDR effect and right before tonemapping so it won’t affect previous effects in the chain.<br />**Note:** Only available with the **High Definition Range** mode. |
| Color Filter  | Tints the render by multiplying a color.                     |
| Hue Shift     | Shifts the hue of all colors.                                |
| Saturation    | Pushes the intensity of all colors.                          |
| Brightness    | Makes the image brighter or darker.<br />**Note:** Only available with the **Low Definition Range** mode. |
| Contrast      | Expands or shrinks the overall range of tonal values.        |

### Channel Mixer

This is used to modify the influence of each input color channel on the overall mix of the output channel. For example, increasing the influence of the green channel on the overall mix of the red channel will adjust all areas of the image containing green (including neutral/monochrome) to become more reddish in hue.


#### Properties

| Property | Function                                                     |
| :-------- | :------------------------------------------------------------ |
| Channel  | Selects the output channel to modify.                        |
| Red      | Modifies the influence of the red channel within the overall mix. |
| Green    | Modifies the influence of the green channel within the overall mix. |
| Blue     | Modifies the influence of the blue channel within the overall mix. |

### Trackballs

The trackballs are used to perform three-way color grading. Adjusting the position of the point on the trackball will have the effect of shifting the hue of the image towards that color in the given tonal range. Different trackballs are used to affect different ranges within the image. Adjusting the slider under the trackball offsets the color lightness of that range.

> **Note:** you can right-click a trackball to reset it to its default value. You can also change the trackballs sensitivity by going to `Edit -> Preferences -> PostProcessing`.


#### Properties

| Property | Function                             |
| :-------- | :------------------------------------ |
| Lift     | Adjusts the dark tones (or shadows). |
| Gamma    | Adjusts the mid-tones.               |
| Gain     | Adjusts the highlights.              |

### Grading Curves

Grading curves are an advanced way to adjust specific ranges in hue, saturation or luminosity in your image. By adjusting the curves on the eight available graphs you can achieve the effects of specific hue replacement, desaturating certain luminosities and much more.

#### YRGB Curves

These curves, also called `Master`, `Red`, `Green` and `Blue` affect the selected input channels intensity across the whole image. The X axis of the graph represents input intensity and the Y axis represents output intensity for the selected channel. This can be used to further adjust the appearance of basic attributes such as contrast and brightness.

>  **Note:** these curves are only available with the **Low Definition Range** mode.

#### Hue vs Hue

Used to shift hues within specific ranges. This curve shifts the input hue (X axis) according to the output hue (Y axis). This can be used to fine tune hues of specific ranges or perform color replacement.


#### Hue vs Sat

Used to adjust saturation of hues within specific ranges. This curve adjusts saturation (Y axis) according to the input hue (X axis). This can be used to tone down particularly bright areas or create artistic effects such as monochromatic except a single dominant color.


#### Sat vs Sat

Used to adjust saturation of areas of certain saturation. This curve adjusts saturation (Y axis) according to the input saturation (X axis). This can be used to fine tune saturation adjustments made with settings from the **Tone** section.


#### Lum vs Sat

Used to adjust saturation of areas of certain luminance. This curve adjusts saturation (Y axis) according to the input luminance (X axis). This can be used to desaturate areas of darkness to provide an interesting visual contrast.

## Deferred Fog

Fog is the effect of overlaying a color onto objects dependent on the distance from the camera. This is used to simulate fog or mist in outdoor environments and is also typically used to hide clipping of objects when a camera’s far clip plane has been moved forward for performance.

The Fog effect creates a screen-space fog based on the camera’s [depth texture](https://docs.unity3d.com/Manual/SL-DepthTextures.html). It supports Linear, Exponential and Exponential Squared fog types. Fog settings should be set in the **Scene** tab of the **Lighting** window.

### Properties

| Property       | Function                          |
| :-------------- | :--------------------------------- |
| Exclude Skybox | Should the fog affect the skybox? |

### Details

This effect will only show up in your **Post-process Layer** if the camera is set to render with the **Deferred rendering path**. It is enabled by default and adds the support of **Fog** from the **Lighting** panel (which would only work with the **Forward rendering path** otherwise).

### Requirements

- Depth texture
- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Depth of Field

**Depth of Field** is a common post-processing effect that simulates the focus properties of a camera lens. In real life, a camera can only focus sharply on an object at a specific distance; objects nearer or farther from the camera will be somewhat out of focus. The blurring not only gives a visual cue about an object’s distance but also introduces Bokeh which is the term for pleasing visual artifacts that appear around bright areas of the image as they fall out of focus.

### Properties

| Property       | Function                                                     |
| :-------------- | :------------------------------------------------------------ |
| Focus Distance | Distance to the point of focus.                              |
| Aperture       | Ratio of the aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is. |
| Focal Length   | Distance between the lens and the film. The larger the value is, the shallower the depth of field is. |
| Max Blur Size  | Convolution kernel size of the bokeh filter, which determines the maximum radius of bokeh. It also affects the performance (the larger the kernel is, the longer the GPU time is required). |

### Performances

The speed of Depth of Field is tied to `Max Blur Size`. Using a value higher than `Medium` is only recommended for desktop computers and, depending on the post-processing budget of your game, consoles. Mobile platforms should stick to the lowest value.

### Requirements

- Depth texture
- Shader Model 3.5

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Grain

Film grain is the random optical texture of photographic film due to the presence of small particles of the metallic silver (or dye clouds for colored films) in the film stock.

The **Grain** effect is based on a coherent gradient noise. It is commonly used to emulate the apparent imperfections of film and often exaggerated in horror themed games.

> **TODO:** before/after screenshot

> **TODO:** editor UI screenshot

### Properties

| Property               | Function                                                     |
| :---------------------- | :------------------------------------------------------------ |
| Colored                | Enables the use of colored grain.                            |
| Intensity              | Grain strength. Higher means more visible grain.             |
| Size                   | Grain particle size.                                         |
| Luminance Contribution | Controls the noisiness response curve based on scene luminance. Lower values mean less noise in dark areas. |

### Performances

Disabling `Colored` will make the effect run faster.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Lens Distortion

This effect simulates the shape of a lens by distorting or undistorting the final rendered picture.


### Properties

| Property     | Function                                                     |
| :------------ | :------------------------------------------------------------ |
| Intensity    | Total distortion amount.                                     |
| X Multiplier | Intensity multiplier on X axis. Set it to 0 to disable distortion on this axis. |
| Y Multiplier | Intensity multiplier on Y axis. Set it to 0 to disable distortion on this axis. |
| Center X     | Distortion center point (X axis).                            |
| Center Y     | Distortion center point (Y axis).                            |
| Scale        | Global screen scaling.                                       |

### Known issues and limitations

- Lens distortion doesn't support AR/VR.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Motion Blur

**Motion Blur** is a common post-processing effect that simulates the blurring of an image when objects filmed by a camera are moving faster than the camera’s exposure time. This can be caused by rapidly moving objects or a long exposure time. **Motion Blur** is used to subtle effect in most types of games but exaggerated in some genres, such as racing games.

> **TODO:** before/after screenshot

> **TODO:** editor UI screenshot

### Properties

| Property      | Function                                                     |
| :------------- | :------------------------------------------------------------ |
| Shutter Angle | The angle of the rotary shutter. Larger values give longer exposure therefore a stronger blur effect. |
| Sample Count  | The amount of sample points, which affects quality and performances. |

### Performances

Using a lower `Sample Count` will lead to better performances.

### Known issues and limitations

- Motion blur doesn't support AR/VR.

### Requirements

- Motion vectors
- Depth texture
- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Screen-space reflections

**Screen-space Reflection** is a technique for reusing screen-space data to calculate reflections. It is commonly used to create more subtle reflections such as on wet floor surfaces or in puddles. Because it fully works in screen-space it can only reflect what's currently on the screen (no backface reflection, no object living outside of the screen).

**Screen-space Reflection** is an expensive technique, but when used correctly can give great results. It is only available in the [deferred rendering path](https://docs.unity3d.com/Manual/RenderTech-DeferredShading.html) as it relies on the Normals G-Buffer.

The current implementation of **Screen-space reflections** in Unity is tuned for performance over quality to make it usable in production on current-gen consoles and desktop computers. Be aware that this technique isn't meant to be used to get perfectly smooth reflections, you should use probes or planar reflections of that. This effect is also great at acting as a specular occlusion effect by limiting the amount of specular light leaking.

> **TODO:** before/after screenshot

> **TODO:** editor UI screenshot

### Properties

| Property                | Function                                                     |
| :----------------------- | :------------------------------------------------------------ |
| Preset                  | Quality presets. Use `Custom` if you want to fine tune it.   |
| Maximum Iteration Count | Maximum number of steps in the raymarching pass. Higher values mean more reflections.<br />**Note:** only available with the `Custom` preset. |
| Thickness               | Ray thickness. Lower values are more expensive but allow the effect to detect smaller details.<br />**Note:** only available with the `Custom` preset. |
| Resolution              | Changes the size of the internal buffer. Downsample it to maximize performances or supersample it to get slow but higher quality results.<br />**Note:** only available with the `Custom` preset. |
| Maximum March Distance  | Maximum distance to traverse in the scene after which it will stop drawing reflections. |
| Distance Fade           | Fades reflections close to the near plane. This is useful to hide common artifacts. |
| Vignette                | Fades reflections close to the screen edges.                 |

### Performances

You should only use the `Custom` preset for beauty shots. On consoles, don't go higher than `Medium` unless you have plenty of GPU time to spare, especially when working at full-hd resolutions. On lower resolutions you can boost the quality preset and get similar timings with a higher visual quality.

### Known issues and limitations

- Screen-space reflections doesn't support AR/VR.

### Requirements

- Compute shader
- Motion vectors
- Deferred rendering path
- Shader Model 5.0

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Vignette

In Photography, vignetting is the term used for the darkening and/or desaturating towards the edges of an image compared to the center. This is usually caused by thick or stacked filters, secondary lenses, and improper lens hoods. It is also often used for artistic effect, such as to draw focus to the center of an image.

> **TODO:** before/after screenshot

The Vignette effect in the post-processing stack comes in 2 modes:

- Classic
- Masked

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Classic

Classic mode offers parametric controls for the position, shape and intensity of the Vignette. This is the most common way to use the effect.

> **TODO:** editor UI screenshot

### Properties

| Property   | Function                                                 |
| -------------- | ------------------------------------------------------------ |
| Color      | Vignette color. Use the alpha channel for transparency.      |
| Center     | Sets the vignette center point (screen center is [0.5,0.5]). |
| Intensity  | Amount of vignetting on screen.                              |
| Smoothness | Smoothness of the vignette borders.                          |
| Roundness  | Lower values will make a more squared vignette.              |
| Rounded    | Should the vignette be perfectly round or be dependent on the current aspect ratio? |

## Masked

Masked mode multiplies a custom texture mask over the screen to create a Vignette effect. This mode can be used to achieve less common or irregular vignetting effects.

> **TODO:** editor UI screenshot

### Properties

| Property  | Function                                            |
| :------------- | :------------------------------------------------------- |
| Color     | Vignette color. Use the alpha channel for transparency. |
| Mask      | A black and white mask to use as a vignette.            |
| Intensity | Mask opacity.                                           |

# Scripting

## Manipulating the stack

### Quick Volumes

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

### Fading Volumes

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

### Profile Editing

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

### Additional notes

If you need to instantiate `PostProcessLayer` at runtime you'll need to make sure resources are properly bound to it. After the component has been added, don't forget to call `Init()` on it with a reference to the `PostProcessResources` file as a parameter.

```csharp
var postProcessLayer = gameObject.AddComponent<PostProcessLayer>();
postProcessLayer.Init(resources);
```

### Writing Custom Effects
This framework allows you to write custom post-processing effects and plug them to the stack without having to modify the codebase. Of course, all effects written against the framework will work out-of-the-box with volume blending, and unless you need loop-dependent features they'll also automatically work with upcoming  [Scriptable Render Pipelines](https://github.com/Unity-Technologies/ScriptableRenderLoop)!

Let's write a very simple grayscale effect to show it off.

Custom effects need a minimum of two files: a C# and a HLSL source files (note that HLSL gets cross-compiled to GLSL, Metal and others API by Unity so it doesn't mean it's restricted to DirectX).

> **Note:** this quick-start guide requires moderate knowledge of C# and shader programming. We won't go over every detail here, consider it as an overview more than an in-depth tutorial.

### C##

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

#### Settings

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

#### Renderer

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

### Shader

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

### Effect ordering

Builtin effects are automatically ordered, but what about custom effects? As soon as you create a new effect or import it into your project it'll be added to the `Custom Effect Sorting` lists in the `Post Process Layer` component on your camera(s).

> **TODO:** editor UI screenshot

They will be pre-sorted by injection point but you can re-order these at will. The order is per-layer, which means you can use different ordering schemes per-camera.

### Custom editor

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

### Additional notes

For performance reasons, FXAA expects the LDR luminance value of each pixel to be stored in the alpha channel of its source target. If you need FXAA and wants to inject custom effects at the `AfterStack` injection point, make sure that the last executed effect contains LDR luminance in the alpha channel (or simply copy alpha from the incoming source). If it's not FXAA won't work correctly.