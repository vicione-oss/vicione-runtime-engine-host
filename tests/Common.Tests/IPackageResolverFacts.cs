using System.Threading.Tasks;

namespace ViciOne.ManagedEngine.PackageResolver;

public interface IPackageResolverFacts
{
    interface IResolveAsync
    {
        Task Returns_content_of_packages_Async();
        Task Returns_load_info_of_packages_Async();
        Task Throws_InvalidOperationException_on_empty_name_Async();
        Task Throws_InvalidOperationException_if_name_is_not_available_Async();
        Task Throws_InvalidOperationException_on_empty_version_Async();
        Task Throws_InvalidOperationException_if_version_is_not_available_Async();
        Task Throws_InvalidOperationException_on_version_conflict_Async();
    }
}
