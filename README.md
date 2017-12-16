conan-vs-extension [![Build status][badge-appveyor]][build-appveyor]
==================

Visual Studio 2017 extension to integrate [conan C/C++ package manager][conan]
functionality into any existing project.

The current task is to provide package management comparable to whan NuGet
provides for .NET projects.

Build
-----

To build the project, either build it using Visual Studio or with MSBuild:

```console
$ msbuild /p:DeployExtension=false
```

Usage
-----

Run the project from Visual Studio (we aren't providing the end-user packages
yet). It will create an isolated test environment and load the plugin. After
that, use the "Tools → Invoke AddConanDepends" command. It should invoke the
`conan build` command for the project currently loaded into the IDE.

[build-appveyor]: https://ci.appveyor.com/project/ForNeVeR/conan-vs-extension/branch/master
[conan]: https://www.conan.io/

[badge-appveyor]: https://ci.appveyor.com/api/projects/status/y4srt9dcjxy466f8/branch/master?svg=true
