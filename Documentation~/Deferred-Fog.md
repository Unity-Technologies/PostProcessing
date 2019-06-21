# Deferred Fog

The **Fog** effect overlays a color onto objects depending on how far away they are from the Camera. 

The **Fog** effect creates a screen-space fog based on the camera’s [depth texture](https://docs.unity3d.com/Manual/SL-DepthTextures.html). It supports Linear, Exponential and Exponential Squared fog types. Fog settings are on the **Scene** tab of the **Lighting** window (menu: **Window > Rendering > Lighting Settings**).


![](images/deferredfog.png)


### Properties

| Property       | Function                          |
| :-------------- | :--------------------------------- |
| Enabled        | Enable this checkbox to turn the **Deferred Fog** effect on.|
| Exclude Skybox | Enable this checkbox to exclude fog from the [skybox](https://docs.unity3d.com/Manual/class-Skybox.html) |

### Details

The **Fog** effect only appears in your **Post-process Layer** if the camera is set to render with the **Deferred rendering path**. It is enabled by default and adds the support of **Fog** from the **Lighting** panel (which would otherwise only work with the **Forward rendering path**).

### Requirements

- Depth texture
- Shader Model 3

See the [Graphics Hardware Capabilities and Emulation](https://docs.unity3d.com/Manual/GraphicsEmulation.html) page for further details and a list of compliant hardware.
