# C# Porting Effort

This directory contains an initial C# implementation of the `rqbench` utility. The
original tool is implemented in Go under `cmd/rqbench`. The goal is to port
functionality to C# while maintaining compatibility with the Go version.

The project targets **.NET 6** and builds a console application. Usage is similar to
the Go version. Example build commands:

```sh
dotnet build
```

The resulting executable will behave similarly to the Go `rqbench` command.
