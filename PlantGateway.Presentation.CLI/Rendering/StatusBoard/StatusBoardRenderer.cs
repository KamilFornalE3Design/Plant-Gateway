using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.StatusBoard
{
    public sealed class StatusBoardRenderer
    {
        private int _topRow;
        private bool _initialized;

        public void RenderInitial(StatusBoardState state)
        {
            _topRow = System.Console.CursorTop;
            _initialized = true;
            WriteTable(state);
        }

        public void Update(StatusBoardState state)
        {
            if (!_initialized)
            {
                RenderInitial(state);
                return;
            }

            int currentRow = System.Console.CursorTop;
            System.Console.SetCursorPosition(0, _topRow);

            // Clear a fixed number of lines (table + spacing). Adjust if you change layout.
            ClearTableLines(lineCount: 6);

            WriteTable(state);
            System.Console.SetCursorPosition(0, currentRow);
        }

        private static void WriteTable(StatusBoardState state)
        {
            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("[yellow]Environment[/]");
            table.AddColumn("[yellow]PipeBridge[/]");
            table.AddColumn("[yellow]ConfigWatcher[/]");
            table.AddColumn("[yellow]Machine[/]");
            table.AddColumn("[yellow]User[/]");

            table.AddRow(
                $"[green]{Markup.Escape(state.Environment)}[/]",
                $"[cyan]{Markup.Escape(state.PipeBridge)}[/]",
                state.ConfigWatcherOn ? "[green]ON[/]" : "[red]OFF[/]",
                Markup.Escape(state.Machine),
                Markup.Escape(state.User)
            );

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private static void ClearTableLines(int lineCount)
        {
            int width = System.Console.WindowWidth;

            for (int i = 0; i < lineCount; i++)
            {
                System.Console.SetCursorPosition(0, System.Console.CursorTop);
                System.Console.Write(new string(' ', width));
                if (i < lineCount - 1)
                    System.Console.SetCursorPosition(0, System.Console.CursorTop + 1);
            }
        }
    }
}
