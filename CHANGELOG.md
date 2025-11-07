\# Changelog



\## \[1.1.0] - 2025-11-07

\### Added

\- Introduced \*\*automatic ACES2 setup\*\* via \*\*Tools → ACES2 Setup\*\* for one-click URP configuration.

\- Added automatic detection of missing ACES2 assets (LUT, material, or render features) with guided prompts.

\- Improved onboarding workflow when importing from the Unity Package Manager.



\### Fixed

\- Fixed \*\*gray-screen issue\*\* when disabling the Custom Tonemapper in Volume Profiles.

\- Fixed several \*\*Volume parameter sync bugs\*\*, ensuring sliders and LUT contribution update in real time.

\- Improved reliability when toggling between different URP renderer assets.



\### Changed

\- Updated internal render feature registration for better URP 17+ compatibility.

\- Minor internal optimizations and cleanup for shader and material initialization.



---



\## \[1.0.1] - 2025-11-06

\### Fixed

sample scene missing materials and messy folders



\### Added

\- Added early prototype of the \*\*ACES2 Setup\*\* menu option under Tools.



---



\## \[1.0.0] - 2025-11-05

\### Added

\- Initial release with full \*\*ACES2 Tonemapper\*\* integration for URP.

\- Support for Volume Profiles, baked LUTs, and Custom Renderer Features.

\- Compatible with Unity 6 / URP 17+.



