## Package

The standard way to install post-processing or update it to the latest version is to use the package manager that comes with Unity 2018.1.

> **Note:** if you already installed one of the scriptable render pipelines in your project then the post-processing package will already be installed.

Go to `Window -> Package Manager`, switch the view from `In Project` to `All` and select `Postprocessing` in the list. In the right panel you'll find information about the package and a button to install or update to the latest available version for the currently running version of Unity.

## Sources

You can, if you prefer, use the bleeding edge version of post-processing but be aware that only packaged versions are officially supported and things may break. If you're not familiar with Git we recommend you download [Github Desktop](https://desktop.github.com/) as it's easy to use and integrates well with Github.

First, make sure you don't already have the `Postprocessing` package installed or it will conflict with a source installation. If you do, you can remove it using the package manager (`Window -> Package Manager`).

Then you can use your Git client to clone the [post-processing repository](https://github.com/Unity-Technologies/PostProcessing) into your `Assets` folder. The development branch is `v2` and is conveniently set as the default so you don't need to pull any specific branch unless you want to follow a specific feature being developed in a separate branch.

If you don't want to use a Git client you can also download a zip archive by clicking the green button that says "Clone or download" at the top of the repository and extract it into your project. The benefit of using Git is that you can quickly update to the latest revision without having to download / extract / replace the whole thing again. It's also more error-proof as it will handle moving & removing files correctly.