using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Routing;
using PlantGateway.Presentation.WebApp.Application.Abstractions.Layout;
using PlantGateway.Presentation.WebApp.Application.Contracts.Layout;
using PlantGateway.Presentation.WebApp.Components.Layout;

namespace PlantGateway.Presentation.WebApp.Infrastructure.Layout
{
    /// <summary>
    /// URL-driven layout context provider.
    /// - Initializes context from current URI.
    /// - Listens to LocationChanged to stay in sync with navigation.
    /// - SetCurrent(...) navigates to the base path of the selected context.
    /// </summary>
    public sealed class LayoutContextProvider : ILayoutContextProvider, IDisposable
    {
        // LayoutContextDefinition:
        // (LayoutContextId Id, string DisplayName, Type LayoutType, string BasePath, string? Icon)
        private static readonly LayoutContextDefinition[] _all =
        {
            new(LayoutContextId.Main,    "Main layout", "/",      "layout-template"),
            new(LayoutContextId.Admin,   "Admin layout",   "/admin", "shield"),
            new(LayoutContextId.Docs,    "Docs layout",    "/docs",  "book-open"),
            new(LayoutContextId.Viewer,  "Viewer layout",  "/viewer","eye"),
            new(LayoutContextId.Landing, "Landing layout", "/landing","home")
        };

        private readonly NavigationManager _navManager;
        private LayoutContextId _current;

        public LayoutContextProvider(NavigationManager navManager)
        {
            _navManager = navManager;

            // Initialize from current URL
            var initial = ResolveFromUri(_navManager.Uri);
            _current = initial.Id;

            // React to navigation (links, back/forward, manual URL, etc.)
            _navManager.LocationChanged += OnLocationChanged;
        }

        public IReadOnlyList<LayoutContextDefinition> All => _all;
        public LayoutContextId Current => _current;

        public event Action<LayoutContextId>? OnChanged;

        public LayoutContextDefinition Get(LayoutContextId id) => _all.First(x => x.Id == id);

        public LayoutContextId GetCurrent() => _current;

        /// <summary>
        /// Sets the current context and navigates to its BasePath.
        /// </summary>
        public void SetCurrent(LayoutContextId id)
        {
            var def = Get(id);
            var target = def.BasePath;

            // If we are already in this context and URL matches, do nothing
            if (_current == id && IsUriInContext(_navManager.Uri, def))
                return;

            // Navigate – OnLocationChanged will update _current and raise OnChanged
            _navManager.NavigateTo(target);
        }

        // -------------------- helpers --------------------

        private void OnLocationChanged(object? sender, LocationChangedEventArgs e)
        {
            Serilog.Log.Debug("LayoutContextProvider.LocationChanged: {Uri}", e.Location);

            var resolved = ResolveFromUri(e.Location);

            if (resolved.Id == _current)
                return;

            _current = resolved.Id;
            OnChanged?.Invoke(_current);

            Serilog.Log.Debug("LayoutContextProvider.Current updated to: {Context}", _current);
        }

        private static LayoutContextDefinition ResolveFromUri(string absoluteUri)
        {
            var uri = new Uri(absoluteUri);
            var path = uri.AbsolutePath; // e.g. "/admin/users"

            // Longest BasePath prefix wins – "/admin/users" → "/admin"
            var match = _all
                .OrderByDescending(c => c.BasePath.Length)
                .FirstOrDefault(c => path.StartsWith(c.BasePath, StringComparison.OrdinalIgnoreCase));

            return match ?? _all[0]; // fallback to Main
        }

        private static bool IsUriInContext(string absoluteUri, LayoutContextDefinition ctx)
        {
            var uri = new Uri(absoluteUri);
            return uri.AbsolutePath.StartsWith(ctx.BasePath, StringComparison.OrdinalIgnoreCase);
        }

        public void Dispose()
        {
            _navManager.LocationChanged -= OnLocationChanged;
        }
    }
}
