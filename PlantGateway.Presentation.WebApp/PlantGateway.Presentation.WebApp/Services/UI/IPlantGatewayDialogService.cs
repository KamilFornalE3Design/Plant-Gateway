namespace PlantGateway.Presentation.WebApp.Services.UI
{
    public interface IPlantGatewayDialogService
    {
        Task Info(string message, string title = "Information");
        Task<bool> Confirm(string message, string title = "Confirm");
        Task<string?> Prompt(string message, string title = "Input");
    }
}
