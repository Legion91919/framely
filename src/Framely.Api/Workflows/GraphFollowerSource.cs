using Framely.Feed;
using Framely.Graph;

namespace Framely.Api.Workflows;

/// <summary>Adapter: satisfies the Feed module's follower port from the Graph module's public API.</summary>
internal sealed class GraphFollowerSource(ISocialGraph graph) : IFollowerSource
{
    public IReadOnlyCollection<Guid> FollowersOf(Guid authorId) => graph.FollowersOf(authorId);
}
