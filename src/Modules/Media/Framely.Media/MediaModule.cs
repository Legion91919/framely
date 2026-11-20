using Framely.Media.Internal;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Diagnostics.HealthChecks;
using Microsoft.Extensions.FileProviders;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;

namespace Framely.Media;

/// <summary>Public entry points of the Media module: registration, the local CDN origin and a readiness check.</summary>
public static class MediaModule
{
    public static IServiceCollection AddMediaModule(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddOptions<MediaOptions>()
            .Bind(configuration.GetSection(MediaOptions.Section))
            .Validate(o => o.MaxUploadBytes > 0, "Media:MaxUploadBytes must be positive.")
            .Validate(o => o.OriginRequestPath.StartsWith('/'), "Media:OriginRequestPath must start with '/'.")
            .Validate(o => !string.IsNullOrWhiteSpace(o.RootPath), "Media:RootPath is required.")
            .ValidateOnStart();
        services.AddSingleton<IPhotoStore>(sp =>
        {
            var options = sp.GetRequiredService<IOptions<MediaOptions>>().Value;
            var env = sp.GetRequiredService<IHostEnvironment>();
            return new LocalDiskPhotoStore(Path.Combine(env.ContentRootPath, options.RootPath));
        });
        services.AddSingleton<IPhotoIngest, PhotoIngestService>();
        return services;
    }

    public static IHealthChecksBuilder AddMediaStorageCheck(this IHealthChecksBuilder builder, params string[] tags) =>
        builder.AddCheck<MediaStorageHealthCheck>("media-store", tags: tags);

    /// <summary>
    /// Serves stored photos from disk at <see cref="MediaOptions.OriginRequestPath"/> with immutable cache headers,
    /// the way a CDN origin would. In production the CDN fronts blob storage and this middleware is not used.
    /// </summary>
    public static IApplicationBuilder UseMediaOrigin(this IApplicationBuilder app)
    {
        ArgumentNullException.ThrowIfNull(app);
        var options = app.ApplicationServices.GetRequiredService<IOptions<MediaOptions>>().Value;
        var store = app.ApplicationServices.GetRequiredService<IPhotoStore>();
        Directory.CreateDirectory(store.RootPath);
        return app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(store.RootPath),
            RequestPath = options.OriginRequestPath,
            ServeUnknownFileTypes = false,
            OnPrepareResponse = ctx =>
            {
                ctx.Context.Response.Headers.CacheControl = "public, max-age=31536000, immutable";
                ctx.Context.Response.Headers.XContentTypeOptions = "nosniff";
            },
        });
    }

    /// <summary>Builds the ingest pipeline over a local folder without a host (tests, tools).</summary>
    internal static IPhotoIngest CreateLocal(MediaOptions options, TimeProvider time) =>
        new PhotoIngestService(new LocalDiskPhotoStore(options.RootPath), Options.Create(options), time);
}
