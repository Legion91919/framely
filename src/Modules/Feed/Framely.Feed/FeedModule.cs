using Framely.Feed.Internal;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;

namespace Framely.Feed;

/// <summary>Registers the Feed module. The host must register an <see cref="IFollowerSource"/> adapter.</summary>
public static class FeedModule
{
    public static IServiceCollection AddFeedModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<FeedOptions>()
            .Bind(configuration.GetSection(FeedOptions.Section))
            .Validate(o => o.PageSize is > 0 and <= 100, "Feed:PageSize must be 1-100.")
            .Validate(o => o.MaxTimelineLength >= o.PageSize, "Feed:MaxTimelineLength must be at least one page.")
            .ValidateOnStart();
        services.AddSingleton<IFeedStore>(sp => new InMemoryFeedStore(sp.GetRequiredService<IOptions<FeedOptions>>().Value.MaxTimelineLength));
        services.AddSingleton<IFeedService>(sp => new FeedService(
            sp.GetRequiredService<IFeedStore>(),
            sp.GetRequiredService<IFollowerSource>(),
            sp.GetRequiredService<TimeProvider>(),
            sp.GetRequiredService<IOptions<FeedOptions>>().Value));
        return services;
    }

    internal static IFeedService CreateInMemory(IFollowerSource followers, TimeProvider time, FeedOptions? options = null)
    {
        options ??= new FeedOptions();
        return new FeedService(new InMemoryFeedStore(options.MaxTimelineLength), followers, time, options);
    }
}
