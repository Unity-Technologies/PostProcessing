This guide gives a very quick & basic overview of the new post-processing framework and how to get started with it. A more extensive documentation will come at some point.

## Installation

Clone the `v2` branch of this repository (we recommend the use of [GitHub Desktop](https://desktop.github.com/)) or download it using the `Download` button at the top-right of the screen and copy the `PostProcessing` folder into your project `Assets` folder.

You can also clone it straight to your `Assets` folder if you prefer to save time.

## Post-process Layer

> For maximum awesomeness we recommend that you work in Linear + HDR. It will still work in Gamma + LDR, although it won't look as good with some effects.

First, you need to add a `Post Process Layer` component to the camera you want to apply post-processing to. You can do that by selecting your camera and use one of the following ways:

- Drag the `PostProcessLayer.cs` script from the project panel to the camera.
- Use the menu `Component -> Rendering -> Post Process Layer`.
- Use the `Add Component` button in the inspector.

<p align="center">
[[/images/quickstart-1.png|Quickstart 1]]
</p>

Let's go over the most important settings:

- **Trigger**: by default the camera itself will be assigned to it. This is the transform that will drive the volume blending feature. In some cases you may want to use a transform other than the camera, e.g. for a top down game you'll want the player character to drive the animation instead of the camera transform.
- **Layer**: a mask of layers to consider for volume blending. It allows you to do volume filtering and is especially useful to optimize volume traversal. You should always have your volumes in dedicated layers instead of the default one for best performances. By default it's set to `Nothing` so don't forget to change it or local volumes won't have any effect.
- **Anti-aliasing**: sets an anti-aliasing method for this camera.
- **Stop NaN Propagation**: will kill any invalid / NaN pixel and replace it with black before post-processing is applied.

The `Rendering Features` section holds lighting effects tied to the camera, such as Deferred Fog, Ambient Occlusion or Screen-space reflections.

You'll also find a set of utilities in the `Toolkit` section (see the page about [[External LUT authoring|(v2) External LUT authoring]]) and a `Custom Effect Sorting` section to fine tune the execution order of your [[custom effects|(v2) Writing custom effects]].

Once your layer is set, you can move on to the cool part: volumes.

## Post-process Volume

The way post-processing works in this framework is by using local & global volumes. It allows you to give each volume a priority and a set of effect overrides to automatically blend post-processing settings in your scene. For instance, you could have a light vignette effect set-up globally but when the player enters a cave you would only override the `intensity` setting of the vignette to make it stronger while keeping the rest of the settings intact.

The `Post Process Volume` component can be added to any game object, the camera itself included. But it's generally a good idea to create a dedicated object for each volume. Let's start by creating a global `Post Process Volume`. Create an empty game object and add the component to it. Don't forget to add it to a layer that's being used by the mask set in the `Post Process Layer` component you added to your camera!

<p align="center">
[[/images/quickstart-2.png|Quickstart 2]]
</p>

By default it's completely empty. From there we can do two types of volumes:

- **Global**: a global volume doesn't have any boundary and will be applied to the whole scene. You can of course have several of these in your scene.
- **Local**: a local volume needs a collider or trigger component attached to it to define its boundaries. Any type of 3D collider will work, from cubes to complex convex meshes but we recommend you use simple colliders as much as possible, as meshes can be quite expensive to traverse. Local volumes can also have a `Blend Distance` that represents the outer distance from the volume surface where blending will start.

We want a global volume, so let's enable `Is Global`.

`Weight` can be used to reduce the global contribution of the volume and all its overrides, with `0` being no contribution at all and `1` full contribution.

The `Priority` field defines the volume order in the stack. The higher this number is, the higher priority a volume has.

We also need a to create a profile for this volume (or re-use an existing one). Let's create one by clicking the `New` button. It will be created as an asset file in your project. To edit a profile content you can either select this asset or go to a volume inspector where it will be replicated for easier access. Once a profile as been assigned you'll see a new button appear, `Clone`. This one will duplicate the currently assigned profile and set it on the volume automatically. This can be handy when you want to create quick variations of a same profile (although you should really use the override system if possible).

We can now start adding effect overrides to the stack.

<p align="center">
[[/images/quickstart-3.png|Quickstart 3]]
</p>

Each field has an override checkbox on its left, you'll need to toggle the settings you want to override for this volume before you can edit them. You can quickly toggle them all on or off by using the small `All` and `None` shortcuts at the top left.

The top-left `On/Off` toggle is used to override the active state of the effect itself.

Finally, you can right-click and effect title to show a quick-action menu.

From there you can start adding local volumes with various priorities and blend distances to your scene and see all of them blend automatically.