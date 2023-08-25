using Spectre.Console;
using VoxelRayCasting;

var selection = new SelectionPrompt<string>();
selection.AddChoice("Start");
selection.AddChoice("Exit");

var pick = AnsiConsole.Prompt(selection);
if (pick == "Start")
{
    //var app = new Application();
    //app.Run();
}