namespace PlantGateway.Presentation.WebApp.Features.Data.Models
{
    public class PipelineRunRequest
    {
        public string PlantGatewayCLIPath { get; set; } = "";
        public string Command { get; set; } = "";
        public string XmlPath { get; set; } = "";
        public string Mode { get; set; } = "";
        public string Format { get; set; } = "";
        public string CsysOption { get; set; } = "";
    }
}
