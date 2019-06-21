# Grain

The **Grain** effect emulates the effect that real-world cameras produce where small particles in the camera’s film give the image a coarse, unprocessed effect. The **Grain** effect available in Unity is based on a coherent gradient noise. 


![](images/grain.png)


### Properties

| Property               | Function                                                     |
| :---------------------- | :------------------------------------------------------------ |
| Colored                | Enable the checkbox to use colored grain.                            |
| Intensity              | Set the value of the **Grain** strength. Higher values show more visible grain.             |
| Size                   | Set the value of the **Grain** particle size.                                         |
| Luminance Contribution | Set the value to control the noisiness response curve. This value is based on scene luminance. Lower values mean less noise in dark areas. |

### Performance

Disabling `Colored` will make the Grain effect run faster.

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
