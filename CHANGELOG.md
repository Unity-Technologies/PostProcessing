# Changelog
All notable changes to this package will be documented in this file.

The format is based on [Keep a Changelog](http://keepachangelog.com/en/1.0.0/)
and this project adheres to [Semantic Versioning](http://semver.org/spec/v2.0.0.html).

## [2.0.3-preview] - 2018-03-13

### Fixed
- Disabled debug compute shaders on OpenGL ES3 to avoid crashes on a lot of Android devices.
- NullReferenceException while mixing volumes and global volumes. ([498](https://github.com/Unity-Technologies/PostProcessing/issues/498))

### Changed
- Improved performances when blending between identical textures.

## [2.0.2-preview] - 2018-03-07

This is the first release of *PostProcessing*.
