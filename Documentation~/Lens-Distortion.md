# Lens Distortion

This effect simulates the shape of a lens by distorting or undistorting the final rendered picture.


![](images/screenshot-distortion.png)



![](images/lensdistortion.png)


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