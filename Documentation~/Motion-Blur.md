# Motion Blur

The **Motion Blur** effect blurs an image when GameObjects are moving faster than the camera’s exposure time. For more information on the Motion Blur effect, see the [Motion Blur](https://docs.unity3d.com/Manual/PostProcessing-MotionBlur.html) documentation in the Unity manual.


![](images/motionblur.png)


### Properties

| Property      | Function                                                     |
| :------------- | :------------------------------------------------------------ |
| Shutter Angle | Set the angle of the rotary shutter. Larger values give longer exposure and a stronger blur effect. |
| Sample Count  | Set the value for the amount of sample points. This affects quality and performance. |

### Performance

Using a lower `Sample Count` will improve performance.

### Known issues and limitations

- Motion blur doesn't support AR/VR.

### Requirements

- Motion vectors
- Depth texture
- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
