using System;
using LeagueSharp;
using LeagueSharp.Common;
using System.Collections.Generic;
using SharpDX;

using Color = System.Drawing.Color;

namespace RoyalAssistant
{
    class Program
    {
        static int[] SRExpCumulative = { 0, 280, 660, 1140, 1720, 2400, 3180, 4060, 5040, 6120, 7300, 8580, 9960, 11440, 13020, 14700, 16480, 18360 };
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            //LoadMenu();
            //Temp fix. Retarded one, since new SR has no ID in L# API yet
            
            if (Game.MapId != (GameMapId)11)
            {
                Console.WriteLine("RoyalAssistant: only SR support implemented!");
				return;
            }
            Game.OnGameUpdate += Game_OnGameUpdate;
            Drawing.OnDraw += Drawing_OnDraw;
            Console.WriteLine("RoyalAssistant Loaded!");
        }
        
        private static void Game_OnGameUpdate(EventArgs args)
        {

        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.Level != 18 && !hero.IsMe && hero.IsVisible && !hero.IsDead)
                {   
                    Drawing.DrawLine(
                        new Vector2(hero.HPBarPosition.X+10, hero.HPBarPosition.Y + 42),
                        new Vector2(hero.HPBarPosition.X+10 + 130 * ((180 + 100 * hero.Level + hero.Experience - SRExpCumulative[hero.Level]) / (180 + 100 * hero.Level)),
                            hero.HPBarPosition.Y + 42), 3, Color.Gold);
                   // Drawing.DrawText(hero.HPBarPosition.X, hero.HPBarPosition.Y + 30, Color.IndianRed, (180 + 100 * hero.Level + hero.Experience - SRExpCumulative[hero.Level]) + " " + (180 + 100 * hero.Level));
                }
        }
    }
}
