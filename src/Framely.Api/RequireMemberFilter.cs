using Framely.Graph;

namespace Framely.Api;

/// <summary>
/// Demo identity for slice 1: the caller names themselves with an <c>X-Member-Id</c> header.
/// Replaced by OIDC bearer tokens in the auth slice; endpoints only ever call <see cref="ViewerExtensions.Viewer"/>.
/// </summary>
internal sealed class RequireMemberFilter(IMemberDirectory members) : IEndpointFilter
{
    public const string Header = "X-Member-Id";

    public async ValueTask<object?> InvokeAsync(EndpointFilterInvocationContext context, EndpointFilterDelegate next)
    {
        var http = context.HttpContext;
        if (!Guid.TryParse(http.Request.Headers[Header], out var id) || members.Find(id) is not { } member)
        {
            return Results.Problem(
                statusCode: StatusCodes.Status401Unauthorized,
                title: "Sign in required",
                detail: $"Send a known member id in the {Header} header (demo identity until the auth slice).");
        }

        http.Items[ViewerExtensions.Key] = member;
        return await next(context).ConfigureAwait(false);
    }
}

internal static class ViewerExtensions
{
    public const string Key = "framely.viewer";

    public static Member Viewer(this HttpContext http) =>
        http.Items[Key] as Member ?? throw new InvalidOperationException("RequireMemberFilter did not run.");
}
