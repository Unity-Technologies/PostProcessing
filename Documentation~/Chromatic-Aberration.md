# Chromatic Aberration

The Chromatic Aberration effect mimics the effect a real-world camera produces when its lens fails to join all colors to the same point. Unity provides support for red/blue and green/purple fringing, and you can define fringing colors by using an input texture.

For more information on the **Chromatic Aberration** effect, read the documentation on [Chromatic Aberration](https://docs.unity3d.com/Manual/PostProcessing-ChromaticAberration.html) in the Unity manual.

![](images/chroma.png)


### Properties

| Property     | Function                                                     |
| :------------ | :------------------------------------------------------------ |
| Spectral Lut | Select the texture used for a custom fringing color. When left empty, Unity will use the default texture. |
| Intensity    | Set the strength of the **Chromatic Aberration** effect.                           |
| Fast Mode    | Use a faster variant of **Chromatic Aberration** effect for improved performance. |

### Details

**Chromatic Aberration** uses a `Spectral Lut` input for custom fringing. Four example spectral textures are provided in the repository:

- Red/Blue (Default)
- Blue/Red
- Green/Purple
- Purple/Green

You can create custom spectral textures in any image editing software. While the resolution size of spectral textures are not limited, small sizes such as th 3x1 textures provided work best. 

You can achieve a rougher effect by manually setting the `Filter Mode` of the input texture to `Point (no filter)`.

### Performance

The performance of the **Chromatic Aberration** effect depends on the `Intensity` value. If the `Intensity` value is set high, the render will be slower as it will need more samples to render smooth chromatic aberrations.

Enabling `Fast Mode` is recommended where possible as it's a lot faster, but not as smooth as the regular mode.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
