conan-vs-extension [![Build status][badge-appveyor]][build-appveyor]
==================

Visual Studio 2017/2019 extension to integrate [Conan C/C++ package manager][conan]
functionality into any existing project.

The current task is to provide package management comparable to whan NuGet
provides for .NET projects.

Update 02-06-2019
------------------
Plugin development is now being resumed after being stagnant for the past year. 

The overall goal of the plugin is for Visual Studio to be able to execute Conan automatically as-needed based on the currently loaded solution/project/configuration.  Over time, this could grow to a lot of convenience operations.  However, the primary (first) objective is to run `conan install` which will generate `conanbuildinfo.props` and satisfy the dependencies. 

Thus, the first requirement is to provide users a mechanism for mapping each Solution/Project/Configuration to a corresponding `conan install` command. A strategy has been chosen for this, and is being discussed here: https://github.com/bincrafters/conan-vs-extension/issues/3

Overall, future features and should try to use a similar configuration-file-based strategy to provide maximum configurability and flexibility to the user of the plugin, by making any new conan-related-operations exposed in any toolbar menus and right-click menus configurable and composeable.  This is particular important in the near-term while we are still deciding how the parts should work together, so that new workflow ideas can be tested without requiring code changes and rebuilds of the plugin. 

Usage
-----

To use the plugin, open the `Conan.VisualStudio.sln` file using the Visual
Studio 2017 and invoke the Run command on the developer machine (we aren't
providing the installer packages yet). It will create an isolated Visual Studio
environment and load the plugin.

### Configuration

To use the Conan executable on your local system, the plugin needs to know where
the Conan executable is. It will try to detect that automatically from your
`PATH` environment variable, but you could set that manually for cases when the
automatic detection doesn't work.

To set the Conan executable location, enter "Tools → Options" menu and select
"Conan" settings category.

![Settings window screenshot][screenshot-settings]

### Package installation

The plugin will install the dependencies using the `conan install` command. To
do that, ensure that your `conanfile.txt` is placed in the same directory as
your project file or in any of its' parent directories, and then invoke the
"Tools → Invoke AddConanDepends" menu command.

!["Invoke AddConanDepends" menu item screenshot][screenshot-addconandepends]

It will call `conan install --build missing --update` using the
[`visual_studio_multi` generator][visual_studio_multi]. After that, you'll need
to integrate the resulting property files into your Visual Studio project.

If you need any diagnostic information, please look for `conan/conan.log` file
in the directory with your conanfile.

### Integration with project

The [`visual_studio_multi` generator][visual_studio_multi] creates the
`conan/conanbuildinfo.props` property file that should be integrated into your
`vcxproj` file. To do that, use the "Tools → Integrate into project" menu
command. It will automatically add the corresponding `<Import>` item into the
`vcxproj` file.

!["Integrate into project" menu item screenshot][screenshot-integrate]

### Building without Visual Studio

If you need to build the generated project without Visual Studio (e.g. on a
build server machine), execute the following terminal commands:

```console
$ cd [directory with conanfile]
$ conan install . -g visual_studio_multi --install-folder ./conan -s compiler.version=15 --build missing --update
$ msbuild [usual params here]
```

After calling `conan install` that way, `msbuild` will be able to find all the
dependencies, because it'll be able to use the Conan-generated
`conan/conanbuildinfo.props` file.

Build
-----

To build the Conan Visual Studio plugin, either with Visual Studio (nothing
unusual here), or use MSBuild:

```console
$ msbuild /p:DeployExtension=false
```

Test
----

This project uses [xUnit.net][xunit] tests, please use any compatible test
runner to run the automated tests.

[build-appveyor]: https://ci.appveyor.com/project/ForNeVeR/conan-vs-extension/branch/master
[conan]: https://www.conan.io/
[visual_studio_multi]: http://docs.conan.io/en/latest/reference/generators/visualstudiomulti.html
[xunit]: https://xunit.github.io/

[badge-appveyor]: https://ci.appveyor.com/api/projects/status/y4srt9dcjxy466f8/branch/master?svg=true
[screenshot-addconandepends]: docs/screenshot-addconandepends.png
[screenshot-integrate]: docs/screenshot-integrate.png
[screenshot-settings]: docs/screenshot-settings.png
