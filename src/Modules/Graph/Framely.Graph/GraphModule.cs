using Framely.Graph.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Framely.Graph;

public static class GraphModule
{
    public static IServiceCollection AddGraphModule(this IServiceCollection services)
    {
        services.AddSingleton<IMemberDirectory, InMemoryMemberDirectory>();
        services.AddSingleton<ISocialGraph, InMemorySocialGraph>();
        return services;
    }

    internal static (IMemberDirectory Members, ISocialGraph Graph) CreateInMemory()
    {
        var members = new InMemoryMemberDirectory();
        return (members, new InMemorySocialGraph(members));
    }
}
