using Markdig;

namespace PlantGateway.Presentation.WebApp.Features.Docs.Services
{
    public class DocumentationService : IDocumentationService
    {
        private readonly IWebHostEnvironment _env;

        public DocumentationService(IWebHostEnvironment env)
        {
            _env = env;
        }

        public async Task<MarkdownDocument?> GetMarkdownAsync(string slug)
        {
            var path = Path.Combine(_env.ContentRootPath, "docs", $"{slug}.md");
            if (!File.Exists(path))
                return null;

            var text = await File.ReadAllTextAsync(path);
            var pipeline = new Markdig.MarkdownPipelineBuilder()
                .UseAdvancedExtensions()
                .Build();

            var html = Markdig.Markdown.ToHtml(text, pipeline);

            // first header becomes title
            string? title = text.Split("\n").FirstOrDefault(x => x.StartsWith("#"))?
                .Replace("#", "").Trim();

            return new MarkdownDocument(title, html);
        }
    }

    public record MarkdownDocument(string? Title, string Html);
}
