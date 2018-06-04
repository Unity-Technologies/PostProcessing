In Photography, vignetting is the term used for the darkening and/or desaturating towards the edges of an image compared to the center. This is usually caused by thick or stacked filters, secondary lenses, and improper lens hoods. It is also often used for artistic effect, such as to draw focus to the center of an image.

> **TODO:** before/after screenshot

The Vignette effect in the post-processing stack comes in 2 modes:

- Classic
- Masked

### Requirements

- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.

## Classic

Classic mode offers parametric controls for the position, shape and intensity of the Vignette. This is the most common way to use the effect.

![Vignette - Classic](images/vignette-1.png)

### Properties

| Property   | Function                                                 |
| -------------- | ------------------------------------------------------------ |
| Color      | Vignette color. Use the alpha channel for transparency.      |
| Center     | Sets the vignette center point (screen center is [0.5,0.5]). |
| Intensity  | Amount of vignetting on screen.                              |
| Smoothness | Smoothness of the vignette borders.                          |
| Roundness  | Lower values will make a more squared vignette.              |
| Rounded    | Should the vignette be perfectly round or be dependent on the current aspect ratio? |

## Masked

Masked mode multiplies a custom texture mask over the screen to create a Vignette effect. This mode can be used to achieve less common or irregular vignetting effects.

![Vignette - Masked](images/vignette-2.png)

### Properties

| Property  | Function                                            |
| :------------- | :------------------------------------------------------- |
| Color     | Vignette color. Use the alpha channel for transparency. |
| Mask      | A black and white mask to use as a vignette.            |
| Intensity | Mask opacity.                                           |