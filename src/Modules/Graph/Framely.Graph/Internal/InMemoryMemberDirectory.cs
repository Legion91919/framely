using System.Collections.Concurrent;
using System.Text.RegularExpressions;

namespace Framely.Graph.Internal;

internal sealed partial class InMemoryMemberDirectory : IMemberDirectory
{
    private readonly ConcurrentDictionary<Guid, Member> _members = new();
    private readonly ConcurrentDictionary<string, Guid> _handles = new(StringComparer.Ordinal);

    public Member Register(Guid id, string handle, string displayName, string city)
    {
        if (!HandleRule().IsMatch(handle ?? string.Empty))
        {
            throw new GraphException(GraphError.InvalidHandle, "Handles are 3-30 lowercase letters, digits, dots or underscores.");
        }

        if (!_handles.TryAdd(handle!, id))
        {
            throw new GraphException(GraphError.HandleTaken, $"The handle '{handle}' is taken.");
        }

        var member = new Member(id, handle!, displayName.Trim(), city.Trim(), LocationSharing: false);
        _members[id] = member;
        return member;
    }

    public Member? Find(Guid id) => _members.TryGetValue(id, out var member) ? member : null;

    public IReadOnlyList<Member> All() => _members.Values.OrderBy(m => m.DisplayName, StringComparer.Ordinal).ToList();

    public Member SetLocationSharing(Guid id, bool enabled)
    {
        var current = Find(id) ?? throw new GraphException(GraphError.UnknownMember, "Unknown member.");
        var updated = current with { LocationSharing = enabled };
        _members[id] = updated;
        return updated;
    }

    [GeneratedRegex("^[a-z0-9._]{3,30}$")]
    private static partial Regex HandleRule();
}
