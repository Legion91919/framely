using Framely.Notifications.Internal;
using Microsoft.Extensions.DependencyInjection;

namespace Framely.Notifications;

public static class NotificationsModule
{
    public static IServiceCollection AddNotificationsModule(this IServiceCollection services)
    {
        services.AddSingleton<InMemoryNotifications>();
        services.AddSingleton<INotificationSender>(sp => sp.GetRequiredService<InMemoryNotifications>());
        services.AddSingleton<INotificationInbox>(sp => sp.GetRequiredService<InMemoryNotifications>());
        return services;
    }

    internal static (INotificationSender Sender, INotificationInbox Inbox) CreateInMemory(TimeProvider time)
    {
        var store = new InMemoryNotifications(time);
        return (store, store);
    }
}
