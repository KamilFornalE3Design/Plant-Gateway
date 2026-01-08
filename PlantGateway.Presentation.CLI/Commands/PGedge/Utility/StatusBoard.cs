using Spectre.Console;

namespace SMSgroup.Aveva.Application.CLI.PGedge.Utility
{
    public static class StatusBoard
    {
        private static int _topRow;

        public static string Environment { get; set; } = "Unknown";
        public static string PipeBridge { get; set; } = "BridgeServer";
        public static bool ConfigWatcherOn { get; set; } = false;

        public static void RenderInitial()
        {
            _topRow = Console.CursorTop;
            WriteTable();
        }

        public static void Update()
        {
            int currentRow = Console.CursorTop;
            Console.SetCursorPosition(0, _topRow);
            ClearTableLines(6); // adjust rows if needed
            WriteTable();
            Console.SetCursorPosition(0, currentRow);
        }

        private static void WriteTable()
        {
            var table = new Table().Border(TableBorder.Rounded).Expand();
            table.AddColumn("[yellow]Environment[/]");
            table.AddColumn("[yellow]PipeBridge[/]");
            table.AddColumn("[yellow]ConfigWatcher[/]");
            table.AddColumn("[yellow]Machine[/]");
            table.AddColumn("[yellow]User[/]");

            table.AddRow(
                $"[green]{Environment}[/]",
                $"[cyan]{PipeBridge}[/]",
                ConfigWatcherOn ? "[green]ON[/]" : "[red]OFF[/]",
                System.Environment.MachineName,
                System.Environment.UserName
            );

            AnsiConsole.Write(table);
            AnsiConsole.WriteLine();
        }

        private static void ClearTableLines(int lineCount)
        {
            for (int i = 0; i < lineCount; i++)
            {
                Console.SetCursorPosition(0, _topRow + i);
                Console.Write(new string(' ', Console.WindowWidth));
            }
            Console.SetCursorPosition(0, _topRow);
        }
    }
}
