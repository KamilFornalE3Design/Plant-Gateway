using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.Header
{
    public sealed class CliHeaderRenderer
    {
        private readonly StatusBoardService _statusService;
        private readonly StatusBoardRenderer _boardRenderer;

        public CliHeaderRenderer(StatusBoardService statusService, StatusBoardRenderer boardRenderer)
        {
            _statusService = statusService;
            _boardRenderer = boardRenderer;
        }

        public void RenderInitial()
        {
            TryResizeConsole();

            var state = _statusService.GetCurrentState();
            _boardRenderer.RenderInitial(state);

            RenderBanner();
        }

        public void UpdateBoard()
        {
            var state = _statusService.GetCurrentState();
            _boardRenderer.Update(state);
        }

        private static void RenderBanner()
        {
            AnsiConsole.Write(
                new FigletText("Plant Gateway\nAveva CLI")
                    .Centered()
                    .Color(Spectre.Console.Color.Green));
        }

        private static void TryResizeConsole()
        {
            try
            {
                if (System.Console.LargestWindowWidth >= 120 && System.Console.LargestWindowHeight >= 40)
                    System.Console.SetWindowSize(width: 120, height: 40);
            }
            catch
            {
                // Ignore redirected mode (CI, piping, etc.)
            }
        }
    }
}
