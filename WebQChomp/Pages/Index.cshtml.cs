using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Antiforgery;
using QChompLibrary;
using System;
using System.Collections.Generic;
using System.Text.Json;
using System.Linq;
using System.Threading.Tasks;
using System.IO;

namespace WebQChomp.Pages
{
    public class Input
    {
        public int X { get; set; }
        public int Y { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IWebHostEnvironment _appEnvironment;
        private Field _gameField = new Field(6, 9, (0, 0));
        private AI _easyModel = Train(500, 6, 9);
        private AI _mediumModel = Train(1000, 6, 9);
        private AI _hardModel = Train(2500, 6, 9);

        public IndexModel(IWebHostEnvironment appEnvironment)
        {
            _appEnvironment = appEnvironment;
            //_model = AI.LoadModel(Path.Combine(_appEnvironment.WebRootPath, "6_9_1713_modelv1.dat"));
        }

        public JsonResult OnPostAction([FromBody]Input json)
        {
            _gameField.MakeMove((json.X, json.Y));
            (int Height, int Width) action = _easyModel.ChooseAction(_gameField.Grid, true);
            return new JsonResult(new { Height = action.Height, Width = action.Width });
        }

        static AI Train(int iterations, int h, int w)
        {
            int delta = 0;
            AI player = new AI();

            for (int i = 0; i < iterations; i++)
            {
                // Initialize new game
                Field game = new Field(h, w, (0, 0));

                // Keeping track of last move made by both players
                Dictionary<int, (int[,] State, (int Height, int Width) Action)> last;
                last = new Dictionary<int, (int[,] State, (int Height, int Width) Action)>();

                last[(int)Field.Players.Player1] = (default, default);
                last[(int)Field.Players.Player2] = (default, default);

                // Game loop
                while (true)
                {
                    // Keep current state and action
                    int[,] state = new int[game.GridHeight, game.GridWidth];
                    Array.Copy(game.Grid, 0, state, 0, game.Grid.Length);
                    (int Height, int Width) action = player.ChooseAction(game.Grid, true);

                    // Keep last state and action
                    last[game.Player] = (state, action);

                    // Make move
                    game.MakeMove(action);
                    int[,] newState = game.Grid;

                    // Update q-values with rewards and break after the game is over, otherwise proceed with no reward
                    if (game.Winner != (int)Field.Players.Blank)
                    {
                        player.UpdateModel(state, action, newState, -1);
                        if (last[game.Player].State != null)
                            player.UpdateModel(last[game.Player].State, last[game.Player].Action, newState, 1);

                        break;
                    }
                    else if (last[game.Player].State != null) // Default state value is null
                    {
                        player.UpdateModel(last[game.Player].State, last[game.Player].Action, newState, 0);
                    }
                }

                // Training stats output
                if ((i + 1) % 1000 == 0)
                {
                    int transitions = player.Transitions;

                    Console.WriteLine($"Training game {((i + 1) / 1000)}k... ({transitions} transitions, {transitions - delta} delta)");
                    delta = player.Transitions;
                }
            }

            Console.WriteLine("Done training\n");
            return player;
        }
    }
}
