using System;
using System.Reflection;
using System.Runtime.InteropServices;
using Xunit.Sdk;
using Xunit.v3;

namespace ViciOne.ManagedEngine;

[AttributeUsage(AttributeTargets.Assembly | AttributeTargets.Method | AttributeTargets.Class, AllowMultiple = false, Inherited = true)]
internal sealed class OSTestAttribute : BeforeAfterTestAttribute
{
    public string[]? Supported { get; init; }
    public string[]? Unsupported { get; init; }

    public override void Before(MethodInfo methodUnderTest, IXunitTest test)
    {
        if (Unsupported is not null)
        {
            foreach (var platform in Unsupported)
            {
                if (RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform)))
                    throw SkipException($"Unsupported on {platform}");
            }
        }

        if (Supported is not null)
        {
            foreach (var platform in Supported)
            {
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Create(platform)))
                    throw SkipException($"Only supported on {string.Join(", ", Supported)}");
            }
        }
    }

    /// <summary>
    /// We use the dynamic skip exception message pattern to turn this into a skipped test when it's not running on one of the targeted OSes.
    /// </summary>
    private static XunitException SkipException(FormattableString text) => new($"$XunitDynamicSkip${text}");
}
