# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [Indev]

### Fixed
- On large scenes, the first object you'd add to a profile could throw a `NullReferenceException`. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))

### Changed
- Minor scripting API improvements. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))
- Script-instantiated profiles in volumes are now properly supported in the inspector. ([#530](https://github.com/Unity-Technologies/PostProcessing/pull/530))

## [2.0.5-preview]

### Fixed
- More XR/Switch related fixes.

## [2.0.4-preview]

### Fixed
- Temporal Anti-aliasing creating NaN values in some cases. ([#337](https://github.com/Unity-Technologies/PostProcessing/issues/337))
- Auto-exposure has been fixed to work the same way it did before the full-compute port.
- XR compilation errors on Xbox One & Switch (2018.2).
- `ArgumentNullException` when attempting to get a property sheet for a null shader. ([#515](https://github.com/Unity-Technologies/PostProcessing/pull/515))
- Stop NaN Propagation not working for opaque-only effects.
- HDR color grading had a slight color temperature offset.
- PSVita compatibility.
- Tizen warning on 2018.2.
- Errors in the console when toggling lighting on/off in the scene view when working in Deferred.
- Debug monitors now work properly with HDRP.

### Added
- Contribution slider for the LDR Lut.
- Support for proper render target load/store actions on mobile (2018.2).

### Changed
- Slightly improved speed & quality of Temporal Anti-aliasing.
- Improved volume texture blending.
- Improved support for LDR Luts of sizes other than 1024x32. ([#507](https://github.com/Unity-Technologies/PostProcessing/issues/507))
- Bloom's `Fast Mode` has been made faster.
- Depth of Field focus is now independent from the screen resolution.
- The number of variants for some shaders has been reduced to improve first-build speed. The biggest one, Uber, is down to 576 variants.

## [2.0.3-preview] - 2018-03-13

### Fixed
- Disabled debug compute shaders on OpenGL ES3 to avoid crashes on a lot of Android devices.
- `NullReferenceException` while mixing volumes and global volumes. ([#498](https://github.com/Unity-Technologies/PostProcessing/issues/498))

### Changed
- Improved performances when blending between identical textures.

## [2.0.2-preview] - 2018-03-07

This is the first release of *PostProcessing*.
