# Installation

## Package

To install post-processing or update to the latest version, use the Package Manager that comes with Unity 2018.1.

> **Note:** if you've installed one of the [scriptable render pipelines](https://docs.unity3d.com/Manual/ScriptableRenderPipeline.html) in your project then the post-processing package will already be installed.

Go to `Window > Package Manager` and switch the view from `In Project` to `All`. Select `Postprocessing` in the list. In the right panel you'll find information about the package and a button to install or update to the latest available version for the version of Unity you are running.

## Sources

You can also use the bleeding edge version of post-processing, but only packaged versions are officially supported. If you're not familiar with Git, download [Github Desktop](https://desktop.github.com/) as it's easy to use and integrates well with Github.

Before installing, make sure you don't already have the `Postprocessing` package installed or it will conflict with a source installation. If you have the package already installed, you can remove it using the Package Manager (`Window > Package Manager`).

Use your Git client to clone the [post-processing repository](https://github.com/Unity-Technologies/PostProcessing) into your `Assets` folder. The development branch is `v2` and is set as the default so you don't need to pull any specific branches unless you want to follow a specific feature being developed in a separate branch.

If you don't want to use a Git client you can also download a zip archive by clicking the green button that says "Clone or download" at the top of the repository and extract it into your project. The benefit of using Git is that you can quickly update to the latest revision without having to download / extract / replace the whole package again. It's also more error-proof as it will handle moving and removing files correctly.
