# Brainf*ck to MIPS compiler (Version 2)

Like the old bf2mips compiler, but written in C# and with some minor optimizations.
The build system relies on .NET core.

## Usage

- `dotnet build` to build
- `dotnet clean` to clean project
- `dotnet run -f <inputfile> [-o <outputfile>]` to run. Without `-o` flag, the output is to stdout.
