using Spectre.Console;
using VoxelRayCast;

var selection = new SelectionPrompt<string>();
selection.AddChoice("Start");
selection.AddChoice("Exit");

var pick = AnsiConsole.Prompt(selection);
if (pick == "Start")
{
    AnsiConsole.MarkupLine("[white]starting application..[/]");
    var game = new Game1();
    game.Run();
}