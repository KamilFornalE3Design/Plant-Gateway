using PlantGateway.Presentation.WebApp.Components.Dialogs;

namespace PlantGateway.Presentation.WebApp.Services.UI
{
    public sealed class PlantGatewayDialogService : IPlantGatewayDialogService
    {
        private DialogHost? _host;

        internal void RegisterHost(DialogHost host)
        {
            _host = host;
        }

        public Task Info(string message, string title = "Information")
        {
            if (_host is null)
                return Task.CompletedTask;

            return _host.ShowMessageBox(title, message);
        }

        public Task<bool> Confirm(string message, string title = "Confirm")
        {
            if (_host is null)
                return Task.FromResult(false);

            return _host.ShowConfirm(title, message);
        }

        public Task<string?> Prompt(string message, string title = "Input")
        {
            if (_host is null)
                return Task.FromResult<string?>(null);

            return _host.ShowPrompt(title, message);
        }
    }
}
