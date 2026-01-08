namespace PlantGateway.Presentation.WebApp.Features.Docs.Services
{
    public interface IDocumentationService
    {
        Task<MarkdownDocument?> GetMarkdownAsync(string slug);
    }
}
