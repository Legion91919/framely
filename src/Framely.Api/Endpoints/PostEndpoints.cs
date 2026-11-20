using Framely.Api.Workflows;
using Framely.Media;
using Microsoft.AspNetCore.Mvc;

namespace Framely.Api.Endpoints;

internal static class PostEndpoints
{
    private const long MultipartOverheadBytes = 64 * 1024;

    public static RouteGroupBuilder MapPostEndpoints(this RouteGroupBuilder api, IConfiguration configuration)
    {
        var maxPhoto = configuration.GetValue<long?>("Media:MaxUploadBytes") ?? new MediaOptions().MaxUploadBytes;

        api.MapPost("/posts", CreatePostAsync)
            .Accepts<IFormFile>("multipart/form-data")
            .WithMetadata(new RequestSizeLimitAttribute(maxPhoto + MultipartOverheadBytes))
            .WithFormOptions(multipartBodyLengthLimit: maxPhoto + MultipartOverheadBytes)
            // Identity is a header, not a cookie, so there is no ambient credential to forge (re-enable with cookie auth).
            .DisableAntiforgery();

        return api;
    }

    private static async Task<IResult> CreatePostAsync(
        HttpContext http,
        IFormFile? photo,
        [FromForm] string? caption,
        [FromForm] string? altText,
        [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey,
        PostPhotoWorkflow workflow,
        CancellationToken cancellationToken)
    {
        if (photo is null)
        {
            return TypedResults.ValidationProblem(new Dictionary<string, string[]> { ["photo"] = ["Attach a JPEG photo in the 'photo' field."] });
        }

        await using var content = photo.OpenReadStream();
        var result = await workflow.PostAsync(
            http.Viewer(),
            new PhotoUpload(content, photo.ContentType, photo.Length, photo.FileName),
            caption,
            altText,
            string.IsNullOrWhiteSpace(idempotencyKey) ? null : idempotencyKey.Trim(),
            cancellationToken);

        var body = PostResponse.From(result.Post);
        if (result.Replayed)
        {
            http.Response.Headers["Idempotent-Replayed"] = "true";
            return TypedResults.Ok(body);
        }

        return TypedResults.Created($"/api/posts/{body.Id}", body);
    }
}
