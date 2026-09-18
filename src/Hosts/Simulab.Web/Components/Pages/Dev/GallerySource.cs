using System.Globalization;
using Simulab.Web.Components.Ui;

namespace Simulab.Web.Components.Pages.Dev;

/// <summary>In-memory server source for the gallery table: normal, empty or failing.</summary>
public sealed class GallerySource(Func<int, GallerySample> create, TimeSpan delay)
{
    public const int ItemCount = 60;

    public GallerySourceMode Mode { get; set; } = GallerySourceMode.Normal;

    public async Task<AppTablePage<GallerySample>> LoadAsync(AppTableQuery query, CancellationToken cancellationToken)
    {
        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken);

        if (Mode == GallerySourceMode.Failing)
            throw new InvalidOperationException("Gallery source is set to fail.");

        var items = Mode == GallerySourceMode.Empty
            ? []
            : Enumerable.Range(1, ItemCount).Select(create).ToList();

        IEnumerable<GallerySample> filtered = items;
        if (query.Search is not null)
        {
            var compare = CultureInfo.CurrentCulture.CompareInfo;
            filtered = filtered.Where(item => compare.IndexOf(item.Name, query.Search, CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace) >= 0);
        }

        filtered = query.SortBy switch
        {
            nameof(GallerySample.Questions) => Order(filtered, item => item.Questions, query.Descending),
            nameof(GallerySample.Score) => Order(filtered, item => item.Score, query.Descending),
            nameof(GallerySample.UpdatedAt) => Order(filtered, item => item.UpdatedAt, query.Descending),
            _ => Order(filtered, item => item.Number, query.Descending),
        };

        var matching = filtered.ToList();
        return new AppTablePage<GallerySample>(
            [.. matching.Skip(query.Page * query.PageSize).Take(query.PageSize)],
            matching.Count);
    }

    private static IEnumerable<GallerySample> Order<TKey>(IEnumerable<GallerySample> items, Func<GallerySample, TKey> key, bool descending) =>
        descending ? items.OrderByDescending(key) : items.OrderBy(key);
}
