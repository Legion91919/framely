using Framely.Feed;
using Framely.Graph;
using Framely.Media;
using Microsoft.AspNetCore.Diagnostics;
using Microsoft.AspNetCore.Mvc;

namespace Framely.Api;

/// <summary>Maps module exceptions to RFC 9457 ProblemDetails. Anything unmapped becomes a 500 without a stack trace.</summary>
internal sealed class DomainExceptionHandler(IProblemDetailsService problems) : IExceptionHandler
{
    public async ValueTask<bool> TryHandleAsync(HttpContext httpContext, Exception exception, CancellationToken cancellationToken)
    {
        (int Status, string Title)? mapped = exception switch
        {
            PhotoRejectedException { Reason: PhotoRejection.TooLarge } => (StatusCodes.Status413PayloadTooLarge, "Photo too large"),
            PhotoRejectedException { Reason: PhotoRejection.UnsupportedType or PhotoRejection.NotAJpeg } => (StatusCodes.Status415UnsupportedMediaType, "Unsupported photo"),
            PhotoRejectedException => (StatusCodes.Status400BadRequest, "Invalid photo"),
            FeedException { Error: FeedError.InvalidCursor } => (StatusCodes.Status400BadRequest, "Invalid cursor"),
            FeedException => (StatusCodes.Status400BadRequest, "Invalid post"),
            GraphException { Error: GraphError.UnknownMember } => (StatusCodes.Status404NotFound, "Member not found"),
            GraphException { Error: GraphError.HandleTaken } => (StatusCodes.Status409Conflict, "Handle taken"),
            GraphException => (StatusCodes.Status400BadRequest, "Not allowed"),
            BadHttpRequestException bad => (bad.StatusCode, "Bad request"),
            _ => null,
        };
        if (mapped is not { } m)
        {
            return false;
        }

        httpContext.Response.StatusCode = m.Status;
        return await problems.TryWriteAsync(new ProblemDetailsContext
        {
            HttpContext = httpContext,
            Exception = exception,
            ProblemDetails = new ProblemDetails { Status = m.Status, Title = m.Title, Detail = exception.Message },
        }).ConfigureAwait(false);
    }
}
