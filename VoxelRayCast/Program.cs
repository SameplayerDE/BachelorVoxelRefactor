using Microsoft.Xna.Framework.Input;
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
    int input;
    input = AnsiConsole.Ask<int>("insert a seed (zero triggers default seed): ");
    game.SetSeed(input == 0 ? 101199 : input);
    input = AnsiConsole.Ask<int>("insert a number of voxel per X (1-4098): ");
    game.SetMapX(input);
    input = AnsiConsole.Ask<int>("insert a number of voxel per Y (1-4098): ");
    game.SetMapY(input);
    input = AnsiConsole.Ask<int>("insert a number of voxel per Z (1-4098): ");
    game.SetMapZ(input);
    input = AnsiConsole.Ask<int>("insert a number for the resolution downscaling factor (1-100): ");
    game.SetResolutionDownScaleBy(input);
    game.Run();
}