conan-vs-extension [![Build status][badge-appveyor]][build-appveyor]
==================

Visual Studio 2017 extension to integrate [Conan C/C++ package manager][conan]
functionality into any existing project.

The current task is to provide package management comparable to whan NuGet
provides for .NET projects.

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
your project file, and then invoke the "Tools → Invoke AddConanDepends" menu
command.

!["Invoke AddConanDepends" menu item screenshot][screenshot-addconandepends]

It will call `conan install --build missing --update` using the
[`visual_studio_multi` generator][visual_studio_multi]. After that, you'll need
to integrate the resulting property files into your Visual Studio project.

### Integration with project

The [`visual_studio_multi` generator][visual_studio_multi] creates the
`conan/conanbuildinfo.props` property file that should be integrated into your
`vcxproj` file. To do that, use the "Tools → Integrate into project" menu
command. It will automatically add the corresponding `<Import>` item into the
`vcxproj` file.

!["Integrate into project" menu item screenshot][screenshot-integrate]

### Building without Visual Studio

If you need to build the generated project without Visual Studio (e.g. on the
build server machine), execute the following terminal commands:

```console
$ cd [directory with vcxproj]
$ conan install . -g visual_studio_multi --install-folder ./conan -s compiler.version=15 --build missing --update
$ msbuild [usual params here]
```

After calling `conan install` that way, `msbuild` will be able to find all the
dependencies, because it'll be able to use the Conan-generated
`conan/conanbuildinfo.props` file.

Build
-----

To build the Conan Visual Studio plugin, either build it using Visual Studio or
with MSBuild:

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
