using Framely.Graph;
using Xunit;

namespace Framely.Tests;

public class SocialGraphTests
{
    private readonly IMemberDirectory _members;
    private readonly ISocialGraph _graph;
    private readonly Member _naledi;
    private readonly Member _pieter;

    public SocialGraphTests()
    {
        (_members, _graph) = GraphModule.CreateInMemory();
        _naledi = _members.Register(Guid.NewGuid(), "naledi.frames", "Naledi Dube", "Cape Town");
        _pieter = _members.Register(Guid.NewGuid(), "karoo.pieter", "Pieter van Wyk", "Prince Albert");
    }

    [Fact]
    public void Follow_is_idempotent()
    {
        Assert.Equal(FollowResult.Created, _graph.Follow(_naledi.Id, _pieter.Id));
        Assert.Equal(FollowResult.AlreadyFollowing, _graph.Follow(_naledi.Id, _pieter.Id));
        Assert.Single(_graph.FollowersOf(_pieter.Id));
    }

    [Fact]
    public void Follow_is_one_directional()
    {
        _graph.Follow(_naledi.Id, _pieter.Id);

        Assert.True(_graph.IsFollowing(_naledi.Id, _pieter.Id));
        Assert.False(_graph.IsFollowing(_pieter.Id, _naledi.Id));
        Assert.Empty(_graph.FollowersOf(_naledi.Id));
    }

    [Fact]
    public void A_member_cannot_follow_themselves()
    {
        var ex = Assert.Throws<GraphException>(() => _graph.Follow(_naledi.Id, _naledi.Id));

        Assert.Equal(GraphError.CannotFollowSelf, ex.Error);
    }

    [Fact]
    public void Following_an_unknown_member_fails()
    {
        var ex = Assert.Throws<GraphException>(() => _graph.Follow(_naledi.Id, Guid.NewGuid()));

        Assert.Equal(GraphError.UnknownMember, ex.Error);
    }

    [Fact]
    public void Unfollow_returns_false_when_there_was_nothing_to_remove()
    {
        _graph.Follow(_naledi.Id, _pieter.Id);

        Assert.True(_graph.Unfollow(_naledi.Id, _pieter.Id));
        Assert.False(_graph.Unfollow(_naledi.Id, _pieter.Id));
    }

    [Fact]
    public void Location_sharing_is_off_by_default_and_opt_in()
    {
        Assert.False(_naledi.LocationSharing);

        var updated = _members.SetLocationSharing(_naledi.Id, enabled: true);

        Assert.True(updated.LocationSharing);
        Assert.True(_members.Find(_naledi.Id)!.LocationSharing);
    }

    [Theory]
    [InlineData("ab")]
    [InlineData("Has.Caps")]
    [InlineData("spaces here")]
    [InlineData("this-handle-is-far-too-long-to-be-used")]
    public void Handles_must_be_3_to_30_lowercase_letters_digits_dots_or_underscores(string handle)
    {
        var ex = Assert.Throws<GraphException>(() => _members.Register(Guid.NewGuid(), handle, "Someone", "Durban"));

        Assert.Equal(GraphError.InvalidHandle, ex.Error);
    }

    [Fact]
    public void Handles_are_unique()
    {
        var ex = Assert.Throws<GraphException>(() => _members.Register(Guid.NewGuid(), "naledi.frames", "Other", "Durban"));

        Assert.Equal(GraphError.HandleTaken, ex.Error);
    }
}
