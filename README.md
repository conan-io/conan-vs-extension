#### Build Status
[![Build status](https://ci.appveyor.com/api/projects/status/8ddamkckmfgu618o/branch/master?svg=true)](https://ci.appveyor.com/project/ConanOrgCI/conan-vs-extension/branch/master)

conan-vs-extension
==================

An extension for Visual Studio 2017/2019 which automates the use of [the Conan C/C++ package manager](https://conan.io/) for retrieving dependencies within Visual Studio projects.  

Extension Installation
-----------------------
The extension will soon be published to the Visual Studio marketplace. Please find it there to install. 

Extension Usage
-----------------------
Once the extension is installed, projects simply need to have a `conanfile.txt` or `conanfile.py` added to the solution.  Once one of these files has been added, the `conan-vs-extension` will download all the project dependencies, build them if necessary, and pass the resulting paths and flags to Visual Studio through a generated `.props` file. Furthermore, when changing the Visual Studio project `Configuration` (between `Release` or `Debug`) or `Platform` (between `x64` and `x86`/`Win32` ), the extension will re-run `Conan` automatically with these new settings and download or build the required binaries.  Crucially, after each run of a Conan operation, the extension will offer to refresh your Visual Studio project once the operation is complete.  This refresh will be necessary for intellisense apply all the new preprocessor defintions and flags, and to reflect all the new headers.  

Development and Testing
-----------------------  

#### Prerequisites

In order to be able to build extension from the source, you need Visual Studio 2017 or 2019 installed. All required components Visual Studio components are listed in the [.vsconfig](https://devblogs.microsoft.com/setup/configure-visual-studio-across-your-organization-with-vsconfig/#) file in the repository root. Just open the `Conan.VisualStudio.sln` solution, and IDE will display a prompt to install missing components:

![vsconfig](docs/images/vsconfig.png)

If you want to build the extension yourself and test it locally (perhaps because you are making changes for a PR), you can currently test the extension one of two ways:  Debug Mode and Local VSIX Installation.

#### Debug Mode  
Most likely, you should just run your changes in debug mode.  Open the `Conan.VisualStudio.sln` file using the Visual Studio 2017 or 2019 and  `Run` the project. It will create an isolated Visual Studio environment and load the extension.  
*Note: This can take up to a minute or two.*

#### Local VSIX Installation  
Alternatively, you may want to build the VSIX and share with a few other developers or something.  In that case, just "Build" the `Release` configuration of project in Visual Studio.  You can build from a Developer Command prompt with this command: 

	$ msbuild /p:Configuration=Release


It will output the `.vsix` file to:  

	Conan.VisualStudio\bin\Release\Conan.VisualStudio.vsix
	
From there, you can share and/or install the `.vsix` file as desired. Here is a decent [blog post about working with `.vsix` files manually](https://weblog.west-wind.com/posts/2016/Mar/01/Registering-and-Unregistering-a-VSIX-Extension-from-the-Command-Line#Installing)

#### Using the Extension with Changes

Once you have the Visual Studio environment with the modified extension loaded, or have installed the extension from the `.vsix` file, you can test it by opening the example project:

	Conan.VisualStudio.Examples/ExampleCLI/ExampleCLI.sln

This example has a `conanfile.txt` with a dependency on the popular formatting library `fmt`.  When the project is first opened, Visual Studio is unable to find the `fmt` project dependency.  The extension should immediately run however, and Visual Studio should give you a toolbar dropdown message offering to refresh the project.  After refreshing, the `fmt` headers should be found by the compiler, and the `lib` files should be found by the linker successfully.  


Update 03-14-2019
------------------
Thanks to @sboulema and @SSE4, the extension now provides the minimum required functionality for basic use and initial testing. 

Update 02-06-2019
------------------
Plugin development is now being resumed after being stagnant for the past year. 

The overall goal of the extension is for Visual Studio to be able to execute Conan automatically as-needed based on the currently loaded solution/project/configuration.  Over time, this could grow to a lot of convenience operations.  However, the primary (first) objective is to run `conan install` which will generate `conanbuildinfo.props` and satisfy the dependencies. 

Thus, the first requirement is to provide users a mechanism for mapping each Solution/Project/Configuration to a corresponding `conan install` command. A strategy has been chosen for this, and is being discussed here: https://github.com/conan-io/conan-vs-extension/issues/5

Overall, future features and should try to use a similar configuration-file-based strategy to provide maximum configurability and flexibility to the user of the extension, by making any new conan-related-operations exposed in any toolbar menus and right-click menus configurable and composeable.  This is particular important in the near-term while we are still deciding how the parts should work together, so that new workflow ideas can be tested without requiring code changes and rebuilds of the extension. 

