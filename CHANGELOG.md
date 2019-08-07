# Roadmap

- [ ] Support CMake Projects
- [ ] Support .json file for project-specific configuration
- [ ] Auto-detect changes to conanfiles
- [ ] Output errors to error list tab

Features that have a checkmark are complete and available for
download in the
[nightly build](http://vsixgallery.com/extension/148ffa77-d70a-407f-892b-9ee542346862/).

# Changelog

These are the changes to each version that has been released
on the official Visual Studio extension gallery.

## 1.2.1

**2019-08-07**

- Fix environment before running Conan ([#162](https://github.com/conan-io/conan-vs-extension/pull/162))
- Add menu option 'About' to show all information about the extension ([#158](https://github.com/conan-io/conan-vs-extension/pull/158))
- Show metadata in DLL properties ([#157](https://github.com/conan-io/conan-vs-extension/pull/157))
- Show Conan version in every log before running the install ([#156](https://github.com/conan-io/conan-vs-extension/pull/156))
- Support all MSVC v16 version series ([#155](https://github.com/conan-io/conan-vs-extension/pull/155))
- Add support for Visual Studio 2015 ([#154](https://github.com/conan-io/conan-vs-extension/pull/154))
- Script to automate RC branch creation ([#97](https://github.com/conan-io/conan-vs-extension/pull/97))


## 1.2.0

**2019-07-22**

- Add all available options for 'build_policy' according to Conan documentation ([#142](https://github.com/conan-io/conan-vs-extension/pull/142))
- Validate path to Conan executable ([#136](https://github.com/conan-io/conan-vs-extension/pull/136))
- Add configuration option to customize installation directory ([#134](https://github.com/conan-io/conan-vs-extension/pull/134))
- Capture any unhandled error during install command ([#126](https://github.com/conan-io/conan-vs-extension/pull/126))
- Capture errors from Conan and log them to output window and log file ([#125](https://github.com/conan-io/conan-vs-extension/pull/125))
- New version pattern: "major.minor.patch.build" ([#122](https://github.com/conan-io/conan-vs-extension/pull/122))
- Remove 'conan-vs-settings.json' feature. ([#121](https://github.com/conan-io/conan-vs-extension/pull/121))


## 1.1.0

**2019-06-27**

- Downgrade dependencies to match 15.0.0 (first VS2017) ([#112](https://github.com/conan-io/conan-vs-extension/pull/112))
- Remove visual_studio_multi generator, it is not working for custom configurations ([#110](https://github.com/conan-io/conan-vs-extension/pull/110))
- Document example about conan.config.json ([#109](https://github.com/conan-io/conan-vs-extension/pull/109))
- Add very basic docs ([#106](https://github.com/conan-io/conan-vs-extension/pull/106))
- Update manifest version on each build (will trigger updates) ([#101](https://github.com/conan-io/conan-vs-extension/pull/101))
- Look for matching Conan profile in a config file for the running VS configuration ([#100](https://github.com/conan-io/conan-vs-extension/pull/100))
- Use Visual Studio 2019 image ([#95](https://github.com/conan-io/conan-vs-extension/pull/95))
- Remove unused assemblies ([#94](https://github.com/conan-io/conan-vs-extension/pull/94))
- Publish to OpenVSIX gallery ([#92](https://github.com/conan-io/conan-vs-extension/pull/92))
- Add .vsconfig to install required dependencies to build the extension ([#91](https://github.com/conan-io/conan-vs-extension/pull/91))
- Convert .NET Standard Class libraries to .NET Framework Class libraries ([#89](https://github.com/conan-io/conan-vs-extension/pull/89))


## 1.0.0

**2019-03-14**

- [x] Automatically find conanfiles and Conan executable
- [x] Run conan install and build dependencies as needed
- [x] Inject `.props` file into project for dependency information
- [x] Popup message to refresh project and resolve intellisense
