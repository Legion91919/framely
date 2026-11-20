using System.Net.Http.Headers;
using System.Net.Http.Json;
using Framely.Api;
using Framely.Graph;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Framely.Tests;

/// <summary>Runs the real API on Kestrel (random port) with a temp media folder: real HTTP, real multipart, real disk.</summary>
internal sealed class ApiHarness : IAsyncDisposable
{
    private readonly WebApplication _app;

    private ApiHarness(WebApplication app, HttpClient client, string root)
    {
        _app = app;
        Client = client;
        Root = root;
    }

    public HttpClient Client { get; }

    public string Root { get; }

    public IMemberDirectory Members => _app.Services.GetRequiredService<IMemberDirectory>();

    public ISocialGraph Graph => _app.Services.GetRequiredService<ISocialGraph>();

    public static async Task<ApiHarness> StartAsync(long maxUploadBytes = 64 * 1024)
    {
        var root = Path.Combine(Path.GetTempPath(), "framely-api-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(root);
        var app = FramelyApp.Build(
            new WebApplicationOptions { EnvironmentName = "Testing", ContentRootPath = root },
            builder =>
            {
                builder.WebHost.UseUrls("http://127.0.0.1:0");
                builder.Logging.ClearProviders();
                builder.Configuration.AddInMemoryCollection(new Dictionary<string, string?>
                {
                    ["Media:RootPath"] = Path.Combine(root, "media"),
                    ["Media:MaxUploadBytes"] = maxUploadBytes.ToString(System.Globalization.CultureInfo.InvariantCulture),
                });
            });
        await app.StartAsync();
        var address = app.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>()!.Addresses.First();
        return new ApiHarness(app, new HttpClient { BaseAddress = new Uri(address) }, root);
    }

    public Member AddMember(string handle, string name, bool locationSharing = false)
    {
        var member = Members.Register(Guid.NewGuid(), handle, name, "Cape Town");
        return locationSharing ? Members.SetLocationSharing(member.Id, true) : member;
    }

    public HttpRequestMessage As(Member viewer, HttpMethod method, string url)
    {
        var request = new HttpRequestMessage(method, url);
        request.Headers.Add("X-Member-Id", viewer.Id.ToString());
        return request;
    }

    public async Task<HttpResponseMessage> PostPhotoAsync(Member author, byte[] jpeg, string caption = "Harbour at dawn", string contentType = "image/jpeg", string? idempotencyKey = null)
    {
        var request = As(author, HttpMethod.Post, "/api/posts");
        var file = new ByteArrayContent(jpeg);
        file.Headers.ContentType = new MediaTypeHeaderValue(contentType);
        request.Content = new MultipartFormDataContent
        {
            { file, "photo", "harbour.jpg" },
            { new StringContent(caption), "caption" },
            { new StringContent("Fishing boats in a calm harbour"), "altText" },
        };
        if (idempotencyKey is not null)
        {
            request.Headers.Add("Idempotency-Key", idempotencyKey);
        }

        return await Client.SendAsync(request);
    }

    public async Task<FeedPageDto> FeedAsync(Member viewer, string? cursor = null)
    {
        var url = cursor is null ? "/api/feed" : $"/api/feed?cursor={Uri.EscapeDataString(cursor)}";
        var response = await Client.SendAsync(As(viewer, HttpMethod.Get, url));
        response.EnsureSuccessStatusCode();
        return (await response.Content.ReadFromJsonAsync<FeedPageDto>())!;
    }

    public async ValueTask DisposeAsync()
    {
        Client.Dispose();
        await _app.StopAsync();
        await _app.DisposeAsync();
    }
}

internal sealed record AuthorDto(Guid Id, string Handle, string DisplayName, string City);

internal sealed record PostDto(Guid Id, AuthorDto Author, string ImageUrl, int Width, int Height, string AltText, string Caption, DateTimeOffset PublishedAt);

internal sealed record FeedPageDto(List<PostDto> Items, string? NextCursor);

internal sealed record ProblemDto(string? Title, int? Status, string? Detail, string? Type);
