namespace ViciOne.ManagedEngine.PackageResolver;

/// <summary>
/// Represents a NuGet package source configuration.
/// </summary>
/// <param name="Source">The source URL or path.</param>
/// <param name="Credentials">Optional authentication credentials.</param>
public record NuGetPackageSource(string Source, NuGetBasicAuthentication? Credentials = null);

/// <summary>
/// Represents basic authentication credentials for a NuGet source.
/// </summary>
/// <param name="Username">The username for authentication.</param>
/// <param name="Password">The password for authentication.</param>
/// <param name="PasswordIsClearText">Indicates whether the password is in clear text.</param>
public record NuGetBasicAuthentication(string Username, string Password, bool PasswordIsClearText = false);
