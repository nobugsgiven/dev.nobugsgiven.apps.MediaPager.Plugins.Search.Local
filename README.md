# MediaPager.Plugins.Search.Local

Local-library search provider for [MediaPager](https://github.com/nobugsgiven/MediaPager).

Implements `ISearchProviderPlugin`, federating the unified header search across items
already in the host's library via the `ILibraryQuery` facade. Search-only: playback of
local files stays host-side (this plugin implements no stream interface).
