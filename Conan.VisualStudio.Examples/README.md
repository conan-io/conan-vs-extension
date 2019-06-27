## ExampleCLI

This example has a `conanfile.txt` with a dependency on the popular formatting library `fmt`. When the project is first opened, Visual Studio is unable to find the `fmt` project dependency. The extension should immediately run however, after it the `fmt` headers should be found by the compiler, and the `lib` files should be found by the linker successfully.

## ConanConfigExample

Very basic example to show how a custom configuration can be associated to a Conan profile
using the `conan.config.json` file. In this example the custom profile modifies an
option of the `fmt` library, which in turn introduces a preprocessor definition that
is used in the _main_ function to write additional output.
