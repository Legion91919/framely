using Framely.Feed;
using Framely.Graph;

namespace Framely.Api.Seed;

/// <summary>Development-only invented demo data: nine photographers, follows and 50 posts over the last week.</summary>
internal static class DemoSeeder
{
    /// <summary>Default "viewing as" member in the web app.</summary>
    public static readonly Guid Thandi = Id(1);

    private static readonly (int N, string Handle, string Name, string City)[] People =
    [
        (1, "thandi.m", "Thandi Mahlangu", "Johannesburg"),
        (2, "naledi.frames", "Naledi Dube", "Bo-Kaap, Cape Town"),
        (3, "karoo.pieter", "Pieter van Wyk", "Prince Albert"),
        (4, "ayesha.p", "Ayesha Patel", "Durban"),
        (5, "sipho.sees", "Sipho Ndlovu", "Soweto"),
        (6, "lerato.light", "Lerato Khumalo", "Drakensberg"),
        (7, "mia.analog", "Mia Botha", "Knysna"),
        (8, "kagiso.street", "Kagiso Sello", "Pretoria"),
        (9, "fatima.film", "Fatima Abrahams", "Stellenbosch"),
    ];

    private static readonly (string Caption, string Alt)[] Shots =
    [
        ("First light on Wale Street. The houses wake up one colour at a time.", "Pastel houses on a sloping street at dawn"),
        ("Twenty minutes of nothing, then this. Karoo patience pays.", "Wide dry plain under a pale sun"),
        ("Fishermen on the pier before the surfers arrive.", "Silhouettes on a pier at sunrise"),
        ("Saturday rhythm on Vilakazi Street. One 35mm prime, nothing else.", "Busy street with long evening shadows"),
        ("Amphitheatre in cloud. Waited two hours for the gap.", "Mountain wall half hidden by cloud"),
        ("Lagoon at slack tide.", "Still lagoon reflecting green hills"),
        ("Jacarandas, week one. Purple everything.", "Street lined with purple-flowering trees"),
        ("Vineyard rows after the first rain.", "Rows of vines under a heavy sky"),
        ("Golden hour over the koppies.", "Rocky hills glowing in late sun"),
        ("Low tide, long exposure, cold feet.", "Smooth water around dark rocks"),
        ("Taxi rank at 06:10, steam and headlights.", "Minibus taxis in morning mist"),
        ("Sandstone and a single cloud.", "Orange cliffs under a blue sky"),
    ];

    public static void Seed(IServiceProvider services)
    {
        var members = services.GetRequiredService<IMemberDirectory>();
        var graph = services.GetRequiredService<ISocialGraph>();
        var feed = services.GetRequiredService<IFeedService>();
        var now = services.GetRequiredService<TimeProvider>().GetUtcNow();
        if (members.Find(Thandi) is not null)
        {
            return;
        }

        var byNumber = People.ToDictionary(p => p.N, p => members.Register(Id(p.N), p.Handle, p.Name, p.City));
        members.SetLocationSharing(Id(3), true); // Pieter opted in to location sharing

        // Thandi follows 2-7 but not Kagiso (8) or Fatima (9): their posts must not reach her feed.
        foreach (var n in new[] { 2, 3, 4, 5, 6, 7 })
        {
            graph.Follow(Thandi, Id(n));
        }

        foreach (var (follower, followee) in new[] { (2, 1), (3, 1), (4, 1), (5, 1), (8, 9), (9, 2), (6, 7), (7, 6), (2, 3) })
        {
            graph.Follow(Id(follower), Id(followee));
        }

        // 50 posts spread over the last 7 days, oldest first, so feeds are built by the same fan-out as live posts.
        for (var i = 0; i < 50; i++)
        {
            var author = byNumber[2 + (i % 8)];
            var shot = Shots[i % Shots.Length];
            var (w, h) = (i % 3) switch { 0 => (800, 1000), 1 => (1200, 800), _ => (900, 900) };
            feed.ImportHistorical(
                new PublishPost(
                    new AuthorSnapshot(author.Id, author.Handle, author.DisplayName, author.City),
                    new PhotoRef(PlaceholderArt.DataUri(i + 1, w, h), w, h),
                    shot.Caption,
                    shot.Alt,
                    IdempotencyKey: null),
                now.AddMinutes(-(50 - i) * 197));
        }
    }

    private static Guid Id(int n) => Guid.Parse($"6f2a0000-0000-4000-8000-{n:D12}");
}
