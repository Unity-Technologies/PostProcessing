# Auto Exposure

The **Auto Exposure** effect simulates how the human eye adjusts to changes in brightness in real-time. To do this, it dynamically adjusts the exposure of an image to match its mid-tone.

In Unity, this effect generates a histogram on every frame and filters it to find the average luminance value. This histogram and the **Auto Exposure** effect requires [Compute shader](https://docs.unity3d.com/Manual/class-ComputeShader.html) support.


![](images/autoexposure.png)


### Properties

**Exposure** settings:

| Property              | Function                                                     |
| :--------------------- | :------------------------------------------------------------ |
| Filtering             | Set the lower and upper percentages of the histogram that find a stable average luminance. Values outside of this range will be discarded and won't contribute to the average luminance. |
| Minimum               | Set the minimum average luminance to consider for auto exposure in EV. |
| Maximum               | Set the maximum average luminance to consider for auto exposure in EV. |
| Exposure Compensation | Set the middle-grey value to compensate the global exposure of the scene. |

**Adaptation** settings:

| Property   | Function                                                     |
| :---------- | :------------------------------------------------------------ |
| Type       | Select the Adaptation type. **Progressive** animates the Auto Exposure. **Fixed** does not animate the Auto Exposure. |
| Speed Up   | Set the Adaptation speed from a dark to a light environment.         |
| Speed Down | Set the Adaptation speed from a light to a dark environment.         |

### Details

Use the `Filtering` range to exclude the darkest and brightest part of the image so that very dark and very bright pixels do not contribute to the average luminance. Values are in percent.

`Minimum`/`Maximum` values clamp the computed average luminance into a given range.

You can set the `Type` to `Fixed` and it will behave like an auto-exposure setting.

You can debug the exposure in your scene with the **Post-process Debug** component. To do this, add the **Post-process Debug** component to your Camera and enable the **Light Meter** monitor.

The Light Meter monitor creates a logarithmic histogram that appears in the **Game** window. This displays information about the exposure in your scene in real time. For more information, see [Debugging](Debugging). 

![](Images/Ppv2 _ Debugging_Light meter_Graph.png)

The Light Meter monitor.

### Requirements

- Compute shader
- Shader model 5
