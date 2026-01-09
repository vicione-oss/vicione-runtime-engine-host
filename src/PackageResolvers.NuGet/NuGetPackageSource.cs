namespace ViciOne.ManagedEngine.PackageResolver;

public record NuGetPackageSource(string Source, NuGetBasicAuthentication? Credentials = null);
public record NuGetBasicAuthentication(string Username, string Password, bool PasswordIsClearText = false);
