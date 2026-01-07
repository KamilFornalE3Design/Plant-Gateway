using Spectre.Console;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Presentation.CLI.Rendering.Console
{
    /// <summary>
    /// Centralized renderer for writing a single Spectre.Console markup line.
    /// Use this instead of calling AnsiConsole.MarkupLine directly.
    /// </summary>
    public sealed class MarkupLineRenderer
    {
        public void Write(string markup)
        {
            AnsiConsole.MarkupLine(markup);
        }

        public void Info(string message)
        {
            AnsiConsole.MarkupLine($"[grey]{Escape(message)}[/]");
        }

        public void Success(string message)
        {
            AnsiConsole.MarkupLine($"[green]✔ {Escape(message)}[/]");
        }

        public void Warning(string message)
        {
            AnsiConsole.MarkupLine($"[yellow]⚠ {Escape(message)}[/]");
        }

        public void Error(string message)
        {
            AnsiConsole.MarkupLine($"[red]✖ {Escape(message)}[/]");
        }

        public void Muted(string message)
        {
            AnsiConsole.MarkupLine($"[dim]{Escape(message)}[/]");
        }

        private static string Escape(string text)
        {
            return Markup.Escape(text);
        }
    }
}
