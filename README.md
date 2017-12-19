conan-vs-extension [![Build status][badge-appveyor]][build-appveyor]
==================

Visual Studio 2017 extension to integrate [Conan C/C++ package manager][conan]
functionality into any existing project.

The current task is to provide package management comparable to whan NuGet
provides for .NET projects.

Usage
-----

Run the project from Visual Studio (we aren't providing the end-user packages
yet). It will create an isolated test environment and load the plugin.

### Configuration

Enter "Tools → Options" menu and select "Conan" settings category. There you'll
be able to set up the path to the Conan executable in your system (if it wasn't
detected automatically).

![Settings window screenshot][screenshot-settings]

### Installing the dependencies

After that, use the "Tools → Invoke AddConanDepends" command. It should invoke
the `conan build` command for the project currently loaded into the IDE.

### Using the tool window

To show the plugin tool window, use the "View → Conan Package Management" menu
item.

Build
-----

To build the project, either build it using Visual Studio or with MSBuild:

```console
$ msbuild /p:DeployExtension=false
```

Test
----

This project uses [xUnit.net][xunit] tests, please use any compatible test
runner to run the automated tests.

[build-appveyor]: https://ci.appveyor.com/project/ForNeVeR/conan-vs-extension/branch/master
[conan]: https://www.conan.io/
[xunit]: https://xunit.github.io/

[badge-appveyor]: https://ci.appveyor.com/api/projects/status/y4srt9dcjxy466f8/branch/master?svg=true
[screenshot-settings]: docs/screenshot-settings.png
