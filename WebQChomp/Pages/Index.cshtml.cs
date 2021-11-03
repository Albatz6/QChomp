﻿using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using QChompLibrary;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using Microsoft.AspNetCore.Http;
using Newtonsoft.Json;
using Microsoft.Extensions.Caching.Memory;

namespace WebQChomp.Pages
{
    // Used for JSON serialization/deserialization when saving complex objects to the user's session
    public static class SessionExtensions
    {
        public static void SetObject(this ISession session, string key, object value)
        {
            session.SetString(key, JsonConvert.SerializeObject(value));
        }

        public static T GetObject<T>(this ISession session, string key)
        {
            var value = session.GetString(key);
            return value == null ? default(T) : JsonConvert.DeserializeObject<T>(value);
        }
    }

    // User input class
    public class Input
    {
        public int X { get; set; }
        public int Y { get; set; }
        public int Diff { get; set; }
        public bool Reset { get; set; }
    }

    public class IndexModel : PageModel
    {
        private readonly IMemoryCache _cache;

        public IndexModel(IMemoryCache cache)
        {
            _cache = cache;
        }

        // Used for debug
        static void PrintField(int[,] field)
        {
            for (int i = 0; i < 6; i++)
            {
                for (int j = 0; j < 9; j++)
                {
                    Debug.Write($"{field[i, j]} ");
                }

                Debug.WriteLine(" ");
            }
        }

        // AJAX-post handler
        public JsonResult OnPostAction([FromBody]Input json)
        {
            // Get AI model
            var model = ModelCache(json.Diff);

            // Try to get game field from user session
            var fieldValue = HttpContext.Session.GetString("_Field");
            var field = (fieldValue == null || json.Reset) ? new Field(6, 9, (0, 0)) : JsonConvert.DeserializeObject<Field>(fieldValue);

            // In case user user decides to reset the game, delete field from session and return without making a move
            if (json.Reset)
            {
                HttpContext.Session.Clear();
                return new JsonResult(new { });
            }

            // Make user move and wait a bit
            field.MakeMove((json.X, json.Y));
            System.Threading.Thread.Sleep(125);
            //Debug.WriteLine("user move");
            //PrintField(field.Grid);

            // Let AI choose a move
            (int Height, int Width) action = (-1, -1);
            if (field.Winner == 0)
            {
                // Don't use epsilon-prob random move when on medium or hard difficulty 
                bool eps = (json.Diff == 0);

                // Limit grid usage per move in range from 1 to 6
                Random rand = new Random();
                action = model.ChooseAction(field.Grid, eps, rand.Next(1, 7));
            }

            // Make move if possible
            if (action.Height != -1 && action.Width != -1)
            {
                field.MakeMove(action);
                //Debug.WriteLine("AI move");
                //PrintField(field.Grid);
            }

            // Update winner and reset game field if the game's ended
            int winner = 0;
            if (field.Winner != 0)
            {
                winner = field.Winner;
                field = new Field(6, 9, (0, 0));
            }

            HttpContext.Session.SetString("_Field", JsonConvert.SerializeObject(field));
            return new JsonResult(new { Height = action.Height, Width = action.Width, Winner = winner });
        }

        // AI model retrieval or caching
        AI ModelCache(int diff)
        {
            AI model;
            string modelName = "6_9_1000_model";

            // Set model name according to difficulty (0 - easy, 2 - hard)
            switch (diff)
            {
                case 0:
                    modelName = "6_9_1000_model";
                    break;

                case 1:
                    modelName = "6_9_5000_model";
                    break;

                case 2:
                    modelName = "6_9_30000_model";
                    break;

                default: break;
            }

            // Try to get cache, otherwise write model to cache
            if (!_cache.TryGetValue(modelName, out model))
            {
                model = AI.LoadModel(@$"Data\{modelName}.json").Item1;

                var cacheEntryOptions = new MemoryCacheEntryOptions()
                    .SetSlidingExpiration(TimeSpan.FromHours(24));

                _cache.Set(modelName, model, cacheEntryOptions);
            }

            return model;
        }
    }
}
