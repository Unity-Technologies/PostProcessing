# Ambient Occlusion

The **Ambient Occlusion** post-processing effect darkens creases, holes, intersections and surfaces that are close to each other.

For further information on the **Ambient Occlusion** effect, refer to the [Ambient Occlusion](https://docs.unity3d.com/Manual/PostProcessing-AmbientOcclusion.html) documentation in the Unity manual.

The **Ambient Occlusion** effect in this package has two modes:

- Scalable Ambient Obscurance
- Multi-scale Volumetric Occlusion

## Scalable Ambient Obscurance

This is a standard implementation of ambient obscurance that works on older platforms. If you need to target a compute-enabled platform, use the **Multi-scale Volumetric Occlusion** mode instead.

### Performance

The **Scalable Ambient Obscurance** mode can be resource-intensive, especially when viewed very close to the Camera. To improve performance, use a low `Radius` setting, to sample pixels that are close and in clip space to the source pixel. This makes cacheing more efficent. Using a higher `Radius` setting generates samples further away from the source pixel and won’t benefit from caching, which slows down the effect. 

Because of the Camera’s perspective, objects near the front plane use larger radiuses than those far away, so computing the ambient occlusion pass for an object close to the camera will be slower than for an object further away that only occupies a few pixels on screen.

Dropping the `Quality` setting down will improve performance too.

**Scalable Ambient Obsurance** should not be used on mobile platforms or consoles as the **Multi-scale Volumetric Occlusion** mode is faster and provides better graphics for these platforms.

### Requirements

- Depth & Normals textures
- Shader model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.


![](images/ssao-1.png)


### Properties

| Property     | Function                                                     |
| :------------ | :------------------------------------------------------------ |
| Intensity    | Adjust the degree of darkness **Ambient Occlusion** produces.                   |
| Radius       | Set the radius of sample points, which controls the extent of darkened areas. |
| Quality      | Define the number of sample points, which affects quality and performance. |
| Color        | Set the tint color of the ambient occlusion.                               |
| Ambient Only | Enable this checkbox to make the **Ambient Occlusion** effect only affect ambient lighting. This option is only available with the Deferred rendering path and HDR rendering. |

## Multi-scale Volumetric Occlusion

This mode is optimized for consoles and desktop platforms. It has better graphics and runs faster than **Scalable Ambient Obscurance** on these platforms but requires [compute shader support](https://docs.unity3d.com/Manual/class-ComputeShader.html).

### Requirements

- Compute shader support
- Shader model 4.5

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

![](images/ssao-2.png)


### Properties

| Property           | Function                                                     |
| :------------------ | :------------------------------------------------------------ |
| Intensity          | Adjust the degree of darkness **Ambient Occlusion** produces.                  |
| Thickness Modifier | Modify the thickness of occluders. This increases dark areas but can introduce dark halos around objects. |
| Color              | Set the tint color of the ambient occlusion.                                 |
| Ambient Only       | Enable this checkbox to make the **Ambient Occlusion** effect only affect ambient lighting. This option is only available with the Deferred rendering path and HDR rendering. |
