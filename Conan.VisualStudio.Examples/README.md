## ExampleCLI

This example has a `conanfile.txt` with a dependency on the popular formatting library `fmt`. When the project is first opened, Visual Studio is unable to find the `fmt` project dependency. The extension should immediately run however, after it the `fmt` headers should be found by the compiler, and the `lib` files should be found by the linker successfully.

