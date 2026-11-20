namespace Framely.Graph;

/// <summary>A Framely member. Location sharing is off by default (POPIA: opt in, never opt out).</summary>
public sealed record Member(Guid Id, string Handle, string DisplayName, string City, bool LocationSharing);

public enum FollowResult
{
    Created,
    AlreadyFollowing,
}

public enum GraphError
{
    UnknownMember,
    CannotFollowSelf,
    InvalidHandle,
    HandleTaken,
}

#pragma warning disable CA1032 // Domain exception: always created with an error code.
public sealed class GraphException(GraphError error, string message) : Exception(message)
#pragma warning restore CA1032
{
    public GraphError Error { get; } = error;
}

/// <summary>Members and their privacy settings.</summary>
public interface IMemberDirectory
{
    Member Register(Guid id, string handle, string displayName, string city);

    Member? Find(Guid id);

    IReadOnlyList<Member> All();

    Member SetLocationSharing(Guid id, bool enabled);
}

/// <summary>Who follows whom. One-directional edges.</summary>
public interface ISocialGraph
{
    FollowResult Follow(Guid followerId, Guid followeeId);

    bool Unfollow(Guid followerId, Guid followeeId);

    bool IsFollowing(Guid followerId, Guid followeeId);

    IReadOnlyCollection<Guid> FollowersOf(Guid memberId);

    IReadOnlyCollection<Guid> Following(Guid memberId);
}
