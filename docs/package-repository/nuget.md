# NuGet

Downloads the requested packages and combines the contents of the packages.

## Known Issues

The content of the packages does not contain the output of `dotnet publish`. Therefore, already installed runtimes are not considered and errors may occur when executing the assembled packages.
