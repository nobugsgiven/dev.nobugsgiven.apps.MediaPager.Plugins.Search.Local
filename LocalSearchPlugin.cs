using MediaPager.App.PluginContracts;

namespace MediaPager.Plugins.Search.Local;

/// <summary>Federates the unified header search across items already in the host's local
/// library. Search-only by design: playback of local files stays host-side (this plugin
/// implements no stream interface, so the host's SSRF surface is untouched).</summary>
public sealed class LocalSearchPlugin(ILibraryQuery library, IPluginHost pluginHost) : ISearchProviderPlugin
{
    public const string SourceKey = "mediapager.search.local";

    public PluginDescriptor Descriptor { get; } = new(
        SourceKey,
        "Local library",
        "0.1.0",
        Author: "Nobugsgiven",
        Description: "Search provider for content already in your library.");

    public async Task<IReadOnlyList<ProviderSearchHit>> SearchAsync(SearchRequest request, CancellationToken cancellationToken)
    {
        var hits = await library.SearchAsync(request.Query ?? "", request.Limit, cancellationToken);
        var tmdb = pluginHost.GetPlugin("mediapager.metadata.tmdb") as IMetadataProviderPlugin;
        var tasks = hits.Select(async hit =>
        {
            EnrichmentData? metadata = null;
            if (tmdb is not null && !string.IsNullOrWhiteSpace(hit.ExternalId) && tmdb.Handles(hit.Kind) &&
                (hit.Year is null || string.IsNullOrWhiteSpace(hit.Overview) ||
                 string.IsNullOrWhiteSpace(hit.ArtworkUrl) || hit.Rating is null))
            {
                try
                {
                    metadata = await tmdb.EnrichAsync(hit.ExternalId, hit.Kind, cancellationToken);
                }
                catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
                {
                    throw;
                }
                catch
                {
                    // Local search remains useful when the metadata provider is unavailable.
                }
            }

            return new ProviderSearchHit(
                hit.Kind,
                hit.ExternalId ?? hit.ItemId,
                hit.Title,
                SourceKey,
                hit.Year ?? metadata?.Year,
                hit.Rating ?? metadata?.Rating,
                string.IsNullOrWhiteSpace(hit.Overview) ? metadata?.Overview : hit.Overview,
                string.IsNullOrWhiteSpace(hit.ArtworkUrl) ? metadata?.ArtworkUrl : hit.ArtworkUrl,
                hit.CatalogItemId,
                hit.CatalogId);
        }).ToList();
        return await Task.WhenAll(tasks);
    }
}
