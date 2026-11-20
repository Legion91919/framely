using Framely.Api.Endpoints;
using Framely.Api.Seed;
using Framely.Api.Workflows;
using Framely.Feed;
using Framely.Graph;
using Framely.Media;
using Framely.Notifications;
using Microsoft.AspNetCore.Diagnostics.HealthChecks;

namespace Framely.Api;

/// <summary>Composition root: wires the four modules, the cross-module workflows, errors, health and the demo seed.</summary>
public static class FramelyApp
{
    public static WebApplication Build(string[] args) => Build(new WebApplicationOptions { Args = args });

    public static WebApplication Build(WebApplicationOptions options, Action<WebApplicationBuilder>? configure = null)
    {
        var builder = WebApplication.CreateBuilder(options);
        configure?.Invoke(builder);

        builder.Services.AddSingleton(TimeProvider.System);
        builder.Services.AddProblemDetails();
        builder.Services.AddExceptionHandler<DomainExceptionHandler>();
        builder.Services.AddHealthChecks().AddMediaStorageCheck("ready");

        builder.Services.AddMediaModule(builder.Configuration);
        builder.Services.AddGraphModule();
        builder.Services.AddFeedModule(builder.Configuration);
        builder.Services.AddNotificationsModule();

        builder.Services.AddSingleton<IFollowerSource, GraphFollowerSource>();
        builder.Services.AddSingleton<PostPhotoWorkflow>();
        builder.Services.AddSingleton<FollowWorkflow>();

        var app = builder.Build();

        app.UseExceptionHandler();
        app.UseStatusCodePages();
        app.UseMediaOrigin();

        app.MapHealthChecks("/health/live", new HealthCheckOptions { Predicate = _ => false });
        app.MapHealthChecks("/health/ready", new HealthCheckOptions { Predicate = c => c.Tags.Contains("ready") });

        var api = app.MapGroup("/api").AddEndpointFilter<RequireMemberFilter>();
        api.MapFeedEndpoints();
        api.MapPostEndpoints(app.Configuration);
        api.MapMemberEndpoints();

        if (app.Environment.IsDevelopment())
        {
            app.MapDemoEndpoints();
            DemoSeeder.Seed(app.Services);
        }

        return app;
    }
}
