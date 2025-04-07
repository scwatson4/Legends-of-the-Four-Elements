# Changelog
All notable changes to this project will be documented in this file.

The format is based on [Keep a Changelog](https://keepachangelog.com/en/1.0.0/),
and this project adheres to [Semantic Versioning](https://semver.org/spec/v2.0.0.html).

## [1.2.0.0] - 2025-02-27
### Added
- Animate Anything window to automatically rig and animate user models.
- Procedural tool to place models in the scene
- New instalation process with package manager
### Fixed
- Removed an extra subfolder that was breaking the gitignore.
- Desktop & ProjectSettings: Updated documentation.
- Packages: Added ForceToStereo for the WAV parser and improved microphone capture.
- Set navMesh objects to the default type.
- Fixed shaders, refactored AnythingVoiceCreator, and corrected the grid system.
- Made the file more uniform and enabled caching.
- Doxyfile: Updated documentation and coding standards. 
- removed unused spline config scripts.
- Integrated GLTFLoader for AWObj to support both files and URLs.

## [1.0.2.4] - 2023-07-05
## Fixed
- Fixed issue where loading behaviour presets at runtime sometimes throws errors.

## [1.0.2.3] - 2023-06-26
### Added
- Added example scene for making objects with the AW scripting API.
### Fixed
- Fixed loading errors in the My World tab.
- Fixed upgrade errors when using Unity 2022.
- Fixed compilation errors.
- Fixed issue where certain make parameters would reset or not be applied properly.

## [1.0.2.2] - 2023-05-25
### Fixed
- Flying and swimming example behaviours now turn off gravity on rigidbodies by default.

## [1.0.2.1] - 2023-05-22
### Fixed
- Fixed issues with some windows not appearing in Mac version.
- Fixed issues assigning monoscripts via editor.
- Namespace fixes.
- Fixed OBJ models being inverted.
- Fixed issue where you couldn't clear text in AI creator.
### Added
- Added check version button to settings panel.

## [1.0.2.0EA] - 2023-04-24
### Added
- Added scale clamping RequestParam for AnythingMaker.
- Added AI creator window.
- Added support for swimming object default behaviours.
- Add CommandHandler factory for requesting commands.
- Added support for mass field from database.

## [1.0.1.0EA] - 2023-02-21
### Fixed
- Fixed bugs in creation.
- Fixed bugs in My World panel.
- Fixed bugs with collections.
- Fixed some compilation issues.
- Fixed some bugs with sign up panel.
### Added
- Added collider options for models.
- Added details panel.
- Added featured models to the browser.
- Added extra default behaviour classes.
- Added extra example behaviour scripts.
- Added settings panel.
- Added serialization for models.
- Added transform panel.
- Added context menu for serialization.
### Changed
- Updated RandomMovement gui handles.

## [1.0.0.1EA] - 2022-11-28
### Fixed
- Fixed bugs with signup panel.
- Fixed style not found bug when opening browser panel.
- Fixed floor positioning bug.
- Resolved issue where searching for "tree" would not return results.

## [1.0.0.0EA] - 2022-11-09
### Added
- First version of the package!
