using System;
using LeagueSharp;
using LeagueSharp.Common;
using System.Collections.Generic;
using System.Diagnostics;
using SharpDX;

using Color = System.Drawing.Color;

namespace RoyalAssistant
{
    class Program
    {
        static Menu menu;
        static int[] SRExpCumulative = { 0, 280, 660, 1140, 1720, 2400, 3180, 4060, 5040, 6120, 7300, 8580, 9960, 11440, 13020, 14700, 16480, 18360 };
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += OnGameLoad;
        }

        static void OnGameLoad(EventArgs args)
        {
            LoadMenu();
            
            if (Game.MapId != (GameMapId)11)
            {
                Console.WriteLine("RoyalAssistant: only SR support implemented!");
				return;
            }

            Drawing.OnDraw += Drawing_OnDraw;
            Game.OnGameEnd += OnGameEnd;
            Console.WriteLine("RoyalAssistant Loaded!");
        }

        static void Drawing_OnDraw(EventArgs args)
        {
            if (Utility.InShopRange() && menu.Item("ward").GetValue<bool>() && !HasWard())
                Drawing.DrawText(400, 600, GetColor(), "Buy a ward, save your life!");
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.Level != 18 && hero.IsVisible && !hero.IsDead)
                {
                    int XOffset;
                    int YOffset;
                    int textXOffset;
                    int textYOffset;
                    int width;
                    if (hero.IsMe && menu.Item("showSelf").GetValue<bool>())
                    {
                        XOffset = 8;
                        YOffset = 2;
                        width = 132;
                        textXOffset = 6;
                        textYOffset = -14;
                    }
                    else if (hero.IsAlly && !hero.IsMe && menu.Item("showAllies").GetValue<bool>() || hero.IsEnemy && menu.Item("showEnemies").GetValue<bool>())
                    {
                        if ((hero.IsAlly && menu.Item("showAlliesT").GetValue<bool>() || hero.IsEnemy && menu.Item("showEnemiesT").GetValue<bool>()))
                        {
                            XOffset = 10;
                            YOffset = 42;
                            width = 130;
                            textXOffset = 6;
                            textYOffset = 3;
                        }
                        else
                        {
                            XOffset = 8;
                            YOffset = 13;
                            width = 132;
                            textXOffset = 6;
                            textYOffset = -2;
                        }
						if(hero.IsAlly)
						{
							YOffset -= 2;
							textYOffset -= 2;
						}
                    }
                    else return;
                    Drawing.DrawLine(
                        new Vector2(hero.HPBarPosition.X + XOffset, hero.HPBarPosition.Y + YOffset),
                        new Vector2(hero.HPBarPosition.X + XOffset + width * ((180 + 100 * hero.Level + hero.Experience - SRExpCumulative[hero.Level]) / (180 + 100 * hero.Level)),
                            hero.HPBarPosition.Y + YOffset), 3, Color.Gold);
                    if (menu.Item("text").GetValue<bool>()) Drawing.DrawText(hero.HPBarPosition.X + textXOffset, hero.HPBarPosition.Y + textYOffset, Color.PaleGoldenrod, (int)(180 + 100 * hero.Level + hero.Experience - SRExpCumulative[hero.Level]) + "/" + (180 + 100 * hero.Level));
                }

        }

        static void OnGameEnd(EventArgs args)
        {
            Process.GetProcessesByName("League of Legends")[0].Kill();
        }

        static bool HasWard()
        {
            foreach (InventorySlot slot in ObjectManager.Player.InventoryItems)
                if (slot.Name.ToLower().Contains("ward") && !slot.Name.ToLower().Contains("trinket"))
                    return true;
            return false;
        }
        
        static Color GetColor()
        {
            switch ((int)((Game.Time % 1) * 10))//SHITTY CODE! :D
            {
                case 0:
                    return Color.IndianRed;
                    break;
                case 1:
                    return Color.LightGoldenrodYellow;
                    break;
                case 2:
                    return Color.Goldenrod;
                    break;
                case 3:
                    return Color.Green;
                    break;
                case 4:
                    return Color.Blue;
                    break;
                case 5:
                    return Color.Violet;
                    break;
                case 6:
                    return Color.DeepPink;
                    break;
                case 7:
                    return Color.DeepSkyBlue;
                    break;
                case 8:
                    return Color.White;
                    break;
                case 9:
                    return Color.Cyan;
                    break;
                default:
                    return Color.ForestGreen;
                    break;
            }
            return Color.Black;
        }

        static void LoadMenu()
        {
            // Initialize the menu
            menu = new Menu("RoyalAssistant", "RoyalAssistant", true);
            menu.AddItem(new MenuItem("showSelf", "Show your XP bar").SetValue(false));
            menu.AddItem(new MenuItem("showAllies", "Show allies XP bar").SetValue(true));
            menu.AddItem(new MenuItem("showEnemies", "Show enemies XP bar").SetValue(true));
            menu.AddItem(new MenuItem("text", "Draw XP count").SetValue(true));
            menu.AddItem(new MenuItem("1", "                  Tracker settings:"));
            //menu.AddItem(new MenuItem("showSelfT", "Tracker showing self").SetValue(false));
            menu.AddItem(new MenuItem("showAlliesT", "Tracker showing allies").SetValue(true));
            menu.AddItem(new MenuItem("showEnemiesT", "Tracker showing enemies").SetValue(true));
            menu.AddItem(new MenuItem("2", "                       Helper:"));
            menu.AddItem(new MenuItem("end", "Quit game on end").SetValue(true));
            menu.AddItem(new MenuItem("ward", "Show \"Buy ward\" reminder").SetValue(true));
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
