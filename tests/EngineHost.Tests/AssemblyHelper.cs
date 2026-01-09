using System;
using System.IO.Abstractions;
using System.Reflection;
using System.Reflection.Metadata;
using System.Reflection.Metadata.Ecma335;
using System.Reflection.PortableExecutable;

namespace ViciOne.ManagedEngine;

internal static class AssemblyHelper
{
    internal static void CreateAssembly(string path, string assemblyName, string assemblyVersion, IFileSystem fileSystem, string? referenceName = default, string? referenceVersion = default)
    {
        var metadata = new MetadataBuilder();

        metadata.AddModule(
            0,
            metadata.GetOrAddString(assemblyName),
            metadata.GetOrAddGuid(new Guid()),
            default,
            default);

        metadata.AddAssembly(
            metadata.GetOrAddString(assemblyName),
            new Version(assemblyVersion),
            default,
            default,
            default,
            AssemblyHashAlgorithm.Sha1);

        if (!string.IsNullOrEmpty(referenceName) && !string.IsNullOrEmpty(referenceVersion))
        {
            metadata.AddAssemblyReference(
                metadata.GetOrAddString(referenceName),
                new Version(referenceVersion),
                default,
                default,
                default,
                default);
        }

        var peBuilder = new ManagedPEBuilder(PEHeaderBuilder.CreateLibraryHeader(), new MetadataRootBuilder(metadata), new BlobBuilder());
        var peBlob = new BlobBuilder();
        peBuilder.Serialize(peBlob);
        using var stream = fileSystem.File.OpenWrite(path);
        peBlob.WriteContentTo(stream);
    }
}
