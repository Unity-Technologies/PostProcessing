# Screen Space Reflections 

The **Screen Space Reflection** effect creates subtle reflections that simulate wet floor surfaces or puddles. Screen Space Reflection is an ideal effect to limit the amount of [specular light](https://docs.unity3d.com/Manual/shader-NormalSpecular.html) leaking. For further information on the **Screen Space Reflection** effect see the [Screen Space Reflection](https://docs.unity3d.com/Manual/PostProcessing-ScreenSpaceReflection.html) documentation in the Unity manual.


![](images/ssr.png)


### Properties

| Property                | Function                                                     |
| :----------------------- | :------------------------------------------------------------ |
| Preset                  | Select the quality preset from the dropdown. Use `Custom` to fine tune the quality.   |
| Maximum Iteration Count (`Custom` preset only) | Set the maximum number of steps in the raymarching pass. Higher values mean more reflections.|
| Thickness (`Custom` preset only)| Set the value of the Ray thickness. Lower values are more resource-intensive but detect smaller details. |
| Resolution (`Custom` preset only)| Select the size of the internal buffer. Select Downsample to maximize performance. Supersample is slower but produces higher quality results. |
| Maximum March Distance  | Set the maximum distance to traverse in the scene after which it will stop drawing reflections. |
| Distance Fade           | Set the value for the distance to fade reflections close to the near plane. This is useful to hide common artifacts. |
| Vignette                | Select the value to fade reflections close to the screen edges.                 |

### Performances

Only use the `Custom` preset for beauty shots. If you are developing for consoles, use `Medium` as the maximum, unless you have plenty of GPU time to spare. On lower resolutions you can boost the quality preset and get similar timings with a higher visual quality.

### Known issues and limitations

- Screen-space reflections doesn't support AR/VR.

### Requirements

- Compute shader
- Motion vectors
- Deferred rendering path
- Shader Model 5.0

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
