using Microsoft.AspNetCore.Components.Web;

namespace PlantGateway.Presentation.WebApp.Services
{
    // Source - https://stackoverflow.com/a
    // Posted by Brian Parker, modified by community. See post 'Timeline' for change history
    // Retrieved 2025-12-05, License - CC BY-SA 4.0

    public static class CustomRenderModes
    {
        public static readonly InteractiveAutoRenderMode InteractiveAutoRenderModeNoPreRender
            = new InteractiveAutoRenderMode(prerender: false);
        public static readonly InteractiveServerRenderMode InteractiveServerRenderModeNoPreRender
            = new InteractiveServerRenderMode(prerender: false);
        public static readonly InteractiveWebAssemblyRenderMode InteractiveWebAssemblyRenderModeNoPreRender
            = new InteractiveWebAssemblyRenderMode(prerender: false);
    }


}
