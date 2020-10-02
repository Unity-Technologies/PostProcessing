# Motion Blur

The **Motion Blur** effect blurs the image in the direction of the **Camera’s** movement. This simulates the blur effect a real-world camera creates when it moves with the lens aperture open, or when it captures an object moving faster than the camera’s exposure time.


![screenshot-motionblur](images\screenshot-motionblur.png)

### Properties

![](images/motionblur.png)

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
