# Depth of Field

**Depth of Field** is a post-processing effect that simulates the focus properties of a camera lens. To learn more about the Depth of Field effect, see the [Depth of Field](https://docs.unity3d.com/Manual/PostProcessing-DepthOfField.html) documentation in the Unity manual.


![](images/dof.png)


### Properties

| Property       | Function                                                     |
| :-------------- | :------------------------------------------------------------ |
| Focus Distance | Set the distance to the point of focus.                              |
| Aperture       | Set the ratio of the aperture (known as f-stop or f-number). The smaller the value is, the shallower the depth of field is. |
| Focal Length   | Set the distance between the lens and the film. The larger the value is, the shallower the depth of field is. |
| Max Blur Size  | Select the convolution kernel size of the bokeh filter from the dropdown. This setting determines the maximum radius of bokeh. It also affects the performance (the larger the kernel is, the longer the GPU time is required). |

### Performance

The speed of Depth of Field is tied to `Max Blur Size`. Only use a value higher than `Medium` if you are developing for desktop computers and, depending on the post-processing budget of your game, consoles. Use the lowest value when developing for mobile platforms.

### Requirements

- Depth texture
- Shader Model 3.5

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
