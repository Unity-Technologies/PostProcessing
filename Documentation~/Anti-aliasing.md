# Anti-aliasing

The **Anti-aliasing** effect softens the appearance of edges in your scene. To do this, it surrounds the edge with similar points of color. This reduces the jagged effect caused by [aliasing](https://en.wikipedia.org/wiki/Aliasing).

![](images/screenshot-antialiasing.png)

The Post-processing anti-aliasing algorithms are image-based, which is useful when support for traditional multisampling is not available, such as the [deferred rendering](https://docs.unity3d.com/Manual/RenderTech-DeferredShading.html) path. You can configure the rendering settings in the [Quality settings](https://docs.unity3d.com/Manual/class-QualitySettings.html) window.

The Post-processing stack offers the following anti-aliasing modes:

- [**Fast Approximate Anti-aliasing (FXAA)**](fast-approximate-anti-aliasing); a fast algorithm for mobile and platforms that don’t support motion vectors.
- [**Subpixel Morphological Anti-aliasing (SMAA)**](subpixel-morphological-anti-aliasing); a high-quality but slower algorithm for mobile and platforms that don’t support motion vectors. 
- [**Temporal Anti-aliasing (TAA)**](temporal-anti-aliasing); an advanced technique which requires motion vectors. Ideal for desktop and console platforms.

Each mode is set per-camera in the **Post-process Layer** component.

<a name="fast-approximate-anti-aliasing"></a>

## Fast Approximate Anti-aliasing (FXAA)

**FXAA** is the most efficient anti-aliasing technique. It's ideal for mobile and other platforms that don’t support motion vectors, which are required for **Temporal Anti-aliasing**.


![](images/aa-1.png)


### Properties

| Property   | Function                                                     |
| :--------- | :----------------------------------------------------------- |
| Fast Mode  | Enable **Fast Mode** for a lower quality but faster variant of FXAA. This option is ideal for mobile platforms. |
| Keep Alpha | Enable **Keep Alpha** to keep the alpha channel untouched by post-processing. If Keep Alpha is disabled, Unity uses the alpha channel to store internal data used to speed up and improve visual quality. |

### Performance

Enable `Fast Mode` if you are developing for mobile or Nintendo Switch to get a performance boost. It will also provide a small boost for PlayStation 4 and Xbox One development. `Fast Mode` does not provide any extra benefits for desktop GPUs. In this case regular mode should be used for added visual quality.

### Requirements

- Shader Model 3

<a name="subpixel-morphological-anti-aliasing"></a>

## Subpixel Morphological Anti-aliasing (SMAA)

**SMAA** is a higher quality anti-aliasing effect than **FXAA** but it's also slower. Depending on the art-style of your game it can work as well as **Temporal Anti-aliasing** while avoiding some of the shortcomings of this technique.


![](images/aa-2.png)


### Properties

| Property | Function                                         |
| :-------- | :------------------------------------------------ |
| Quality  | Set the overall quality of the anti-aliasing filter. |

### Performance

Lowering the `Quality` setting makes the effect run faster. Do not use **SMAA** on mobile platforms.

### Known issues and limitations

- SMAA doesn't support AR/VR.

### Requirements

- Shader Model 3

<a name="temporal-anti-aliasing"></a>

## Temporal Anti-aliasing (TAA)

**TAA** is an advanced anti-aliasing technique where frames are accumulated over time in a history buffer to be used to smooth edges more effectively. It is substantially better at smoothing edges in motion but requires motion vectors and is more expensive than **FXAA**. It is ideal for desktop and console platforms.


![](images/aa-3.png)


### Properties

| Property            | Function                                                     |
| :------------------- | :------------------------------------------------------------ |
| Jitter Spread       | Set the diameter (in texels) over which Unity spreads jitter samples. Smaller values result in crisper but a more aliased output. Larger values result in more stable but blurrier output. |
| Stationary Blending | Set the blend coefficient for stationary fragments. This setting controls the percentage of history sample blended into final color for fragments with minimal active motion. |
| Motion Blending     | Set the blending coefficient for moving fragments. This setting controls the percentage of history sample blended into the final color for fragments with significant active motion. |
| Sharpness           | Set the sharpness to alleviate the slight loss of details in high frequency regions which can be caused by TAA. |

### Known issues and limitations

- Not supported on GLES2 platforms.

### Requirements

- Motion vectors
- Depth texture
- Shader Model 3
