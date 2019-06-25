# Post-processing Stack v2 for Mobile Optimization

This branch is under active development and holds the current version of the post-processing stack. 

Fork版本说明
------------

本分支版本仅支持移动平台，对源代码作了一定的修改。对影响性能的代码作了一定的调整。
使用本库只需要拷贝同级目录下的PostProcessing文件夹，并且对非此文件夹下资源只作保留或删除，不再维护。

不支持SWITCH、PSP、XBOX、TVOS、CONSOLE、PSSL
不支持VR XR AR等虚拟平台
不支持SMAA、TAA抗锯齿效果、FXAA常规效果
不支持延迟渲染
不支持ScalableAO的High,Ultra质量
不支持Multi-scale VO
不支持延迟渲染雾Fog
不支持屏幕空间反射ScreenSpaceReflections
不支持景深的内核大小为大，非常大
不支持Bloom的非Fast Mode
不支持Bloom的镜头污迹效果

或者说，

支持Android、iOS、Standard、Vulkan、Metal
支持Compute Shader
支持FXAA Fast Mode效果
支持前向渲染
支持ScalableAO的Lowest,Low,Medium质量
支持景深的内核大小为小，中等
不支持Bloom的Fast Mode

如果有希望支持的功能，可以一起讨论，再做调整。

已优化的后期效果有：



Instructions
------------

Documentation is available [on the wiki](https://github.com/Unity-Technologies/PostProcessing/wiki).

The current version requires Unity 5.6.1+. Some effects and features are only available on newer versions of Unity.

License
-------

Unity Companion License (see [LICENSE](LICENSE.md))
