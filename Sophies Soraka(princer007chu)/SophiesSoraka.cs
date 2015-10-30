// --------------------------------------------------------------------------------------------------------------------
// <copyright file="SophiesSoraka.cs" company="ChewyMoon">
//   Copyright (C) 2015 ChewyMoon
//   
//   This program is free software: you can redistribute it and/or modify
//   it under the terms of the GNU General Public License as published by
//   the Free Software Foundation, either version 3 of the License, or
//   (at your option) any later version.
//   
//   This program is distributed in the hope that it will be useful,
//   but WITHOUT ANY WARRANTY; without even the implied warranty of
//   MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
//   GNU General Public License for more details.
//   
//   You should have received a copy of the GNU General Public License
//   along with this program.  If not, see <http://www.gnu.org/licenses/>.
// </copyright>
// <summary>
//   The sophies soraka.
// </summary>
// --------------------------------------------------------------------------------------------------------------------
namespace Sophies_Soraka
{
    using System;
    using System.Diagnostics.CodeAnalysis;
    using System.Drawing;
    using System.Linq;

    using LeagueSharp;
    using LeagueSharp.Common;

    internal class SophiesSoraka
    {
        #region Public Properties

        public static Menu Menu { get; set; }
        public static Orbwalking.Orbwalker Orbwalker { get; set; }
        public static bool Packets
        {
            get
            {
                return false;
            }
        }
        public static Spell Q { get; set; }
        public static Spell W { get; set; }
        public static Spell E { get; set; }
        public static Spell R { get; set; }

        #endregion

        #region Public Methods and Operators

        public static void OnGameLoad(EventArgs args)
        {
            if (ObjectManager.Player.ChampionName != "Soraka")
            {
                return;
            }

            Q = new Spell(SpellSlot.Q, 950);
            W = new Spell(SpellSlot.W, 550);
            E = new Spell(SpellSlot.E, 925);
            R = new Spell(SpellSlot.R);

            Q.SetSkillshot(0.283f, 210, 1100, false, SkillshotType.SkillshotCircle);
            E.SetSkillshot(0.5f, 70f, 1750, false, SkillshotType.SkillshotCircle);

            CreateMenu();

            PrintChat("loaded. Reworked by princer007chu(aka Arcane Maniac)");

            Interrupter2.OnInterruptableTarget += InterrupterOnOnPossibleToInterrupt;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloserOnOnEnemyGapcloser;
            Game.OnUpdate += GameOnOnGameUpdate;
            Drawing.OnDraw += DrawingOnOnDraw;
            Orbwalking.BeforeAttack += OrbwalkingOnBeforeAttack;
        }

        public static void PrintChat(string msg)
        {
            Game.PrintChat("<font color='#3492EB'>Sophie's Soraka:</font> <font color='#FFFFFF'>" + msg + "</font>");
        }

        #endregion

        #region Methods
        public static void AntiGapcloserOnOnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            var unit = gapcloser.Sender;

            if (Menu.Item("useQGapcloser").GetValue<bool>() && unit.IsValidTarget(Q.Range) && Q.IsReady())
            {
                Q.Cast(gapcloser.End, Packets);
            }

            if (Menu.Item("useEGapcloser").GetValue<bool>() && unit.IsValidTarget(E.Range) && E.IsReady())
            {
                E.Cast(gapcloser.End, Packets);
            }
        }

        public static void AutoR()
        {
            if (!R.IsReady())
            {
                return;
            }

            if (
                ObjectManager.Get<Obj_AI_Hero>()
                    .Where(x => x.IsAlly && x.IsValidTarget(float.MaxValue, false))
                    .Select(x => (int)x.Health / x.MaxHealth * 100)
                    .Select(
                        friendHealth =>
                        new { friendHealth, health = Menu.Item("autoRPercent").GetValue<Slider>().Value })
                    .Where(x => x.friendHealth <= x.health)
                    .Select(x => x.friendHealth)
                    .Any())
            {
                R.Cast(Packets);
            }
        }

        public static void AutoW()
        {
            if (!W.IsReady())
            {
                return;
            }

            var autoWHealth = Menu.Item("autoWHealth").GetValue<Slider>().Value;
            if (ObjectManager.Player.HealthPercent < autoWHealth)
            {
                return;
            }

            var dontWInFountain = Menu.Item("DontWInFountain").GetValue<bool>();
            if (dontWInFountain && ObjectManager.Player.InFountain())
            {
                return;
            }

            var healthPercent = Menu.Item("autoWPercent").GetValue<Slider>().Value;

            var canidates = ObjectManager.Get<Obj_AI_Hero>().Where(x => x.IsValidTarget(W.Range, false) && x.IsAlly && x.HealthPercent < healthPercent);
            var wMode = Menu.Item("HealingPriority").GetValue<StringList>().SelectedValue;

            switch (wMode)
            {
                case "Most AD":
                    canidates = canidates.OrderByDescending(x => x.TotalAttackDamage);
                    break;
                case "Most AP":
                    canidates = canidates.OrderByDescending(x => x.TotalMagicalDamage);
                    break;
                case "Least Health":
                    canidates = canidates.OrderBy(x => x.Health);
                    break;
                case "Least Health (Prioritize Squishies)":
                    canidates = canidates.OrderBy(x => x.Health).ThenBy(x => x.MaxHealth);
                    break;
            }

            var target = dontWInFountain ? canidates.FirstOrDefault(x => !x.InFountain()) : canidates.FirstOrDefault();

            if (target != null)
            {
                W.CastOnUnit(target);
            }
        }

        public static void Combo()
        {
            var useQ = Menu.Item("useQ").GetValue<bool>();
            var useE = Menu.Item("useE").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
				PredictionOutput prediction;
                float divider = target.Position.Distance(ObjectManager.Player.Position) / Q.Range;
                Q.Delay = 0.2f + 0.8f * divider;
                prediction = Q.GetPrediction(target, true);
                if(prediction.Hitchance >= HitChance.High)
                    Q.Cast(target, Packets);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target, Packets);
            }
        }

        public static void CreateMenu()
        {
            Menu = new Menu("Sophies's Soraka", "sSoraka", true);

            // Target Selector
            var tsMenu = new Menu("Target Selector", "ssTS");
            TargetSelector.AddToMenu(tsMenu);
            Menu.AddSubMenu(tsMenu);

            // Orbwalking
            var orbwalkingMenu = new Menu("Orbwalking", "ssOrbwalking");
            Orbwalker = new Orbwalking.Orbwalker(orbwalkingMenu);
            Menu.AddSubMenu(orbwalkingMenu);

            // Combo
            var comboMenu = new Menu("Combo", "ssCombo");
            comboMenu.AddItem(new MenuItem("useQ", "Use Q").SetValue(true));
            comboMenu.AddItem(new MenuItem("useE", "Use E").SetValue(true));
            Menu.AddSubMenu(comboMenu);

            // Harass
            var harassMenu = new Menu("Harass", "ssHarass");
            harassMenu.AddItem(new MenuItem("useQHarass", "Use Q").SetValue(true));
            harassMenu.AddItem(new MenuItem("useEHarass", "Use E").SetValue(true));
            Menu.AddSubMenu(harassMenu);

            // Healing
            var healingMenu = new Menu("Healing", "ssHeal");

            var wMenu = new Menu("W Settings", "WSettings");
            wMenu.AddItem(new MenuItem("autoW", "Use W").SetValue(true));
            wMenu.AddItem(new MenuItem("autoWPercent", "Ally Health Percent").SetValue(new Slider(50, 1)));
            wMenu.AddItem(new MenuItem("autoWHealth", "My Health Percent").SetValue(new Slider(30, 1)));
            wMenu.AddItem(new MenuItem("DontWInFountain", "Dont W in Fountain").SetValue(true));
            wMenu.AddItem(
                new MenuItem("HealingPriority", "Healing Priority").SetValue(
                    new StringList(
                        new[] { "Most AD", "Most AP", "Least Health", "Least Health (Prioritize Squishies)" }, 
                        3)));
            healingMenu.AddSubMenu(wMenu);

            var rMenu = new Menu("R Settings", "RSettings");
            rMenu.AddItem(new MenuItem("autoR", "Use R").SetValue(true));
            rMenu.AddItem(new MenuItem("autoRPercent", "% Percent").SetValue(new Slider(15, 1)));
            healingMenu.AddSubMenu(rMenu);

            Menu.AddSubMenu(healingMenu);

            // Drawing
            var drawingMenu = new Menu("Drawing", "ssDrawing");
            drawingMenu.AddItem(new MenuItem("drawQ", "Draw Q").SetValue(true));
            drawingMenu.AddItem(new MenuItem("drawW", "Draw W").SetValue(true));
            drawingMenu.AddItem(new MenuItem("drawE", "Draw E").SetValue(true));
            Menu.AddSubMenu(drawingMenu);

            // Misc
            var miscMenu = new Menu("Misc", "ssMisc");
            miscMenu.AddItem(new MenuItem("useQGapcloser", "Q on Gapcloser").SetValue(true));
            miscMenu.AddItem(new MenuItem("useEGapcloser", "E on Gapcloser").SetValue(true));
            miscMenu.AddItem(new MenuItem("eInterrupt", "Use E to Interrupt").SetValue(true));
            miscMenu.AddItem(new MenuItem("AttackMinions", "Attack Minions").SetValue(false));
            miscMenu.AddItem(new MenuItem("AttackChampions", "Attack Champions").SetValue(true));
            Menu.AddSubMenu(miscMenu);

            Menu.AddToMainMenu();
        }

        public static void DrawingOnOnDraw(EventArgs args)
        {
            var drawQ = Menu.Item("drawQ").GetValue<bool>();
            var drawW = Menu.Item("drawW").GetValue<bool>();
            var drawE = Menu.Item("drawE").GetValue<bool>();

            var p = ObjectManager.Player.Position;

            if (drawQ)
            {
                Render.Circle.DrawCircle(p, Q.Range, Q.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawW)
            {
                Render.Circle.DrawCircle(p, W.Range, W.IsReady() ? Color.Aqua : Color.Red);
            }

            if (drawE)
            {
                Render.Circle.DrawCircle(p, E.Range, E.IsReady() ? Color.Aqua : Color.Red);
            }
        }

        public static void GameOnOnGameUpdate(EventArgs args)
        {
            switch (Orbwalker.ActiveMode)
            {
                case Orbwalking.OrbwalkingMode.Mixed:
                    Harass();
                    break;
                case Orbwalking.OrbwalkingMode.Combo:
                    Combo();
                    break;
            }

            if (Menu.Item("autoW").GetValue<bool>())
            {
                AutoW();
            }

            if (Menu.Item("autoR").GetValue<bool>())
            {
                AutoR();
            }
        }

        public static void Harass()
        {
            var useQ = Menu.Item("useQHarass").GetValue<bool>();
            var useE = Menu.Item("useEHarass").GetValue<bool>();
            var target = TargetSelector.GetTarget(Q.Range, TargetSelector.DamageType.Magical);

            if (target == null)
            {
                return;
            }

            if (useQ && Q.IsReady())
            {
                PredictionOutput prediction;
                float divider = target.Position.Distance(ObjectManager.Player.Position) / Q.Range;
                Q.Delay = 0.2f + 0.8f * divider;
                prediction = Q.GetPrediction(target, true);
                if (prediction.Hitchance >= HitChance.High)
                    Q.Cast(target, Packets);
            }

            if (useE && E.IsReady())
            {
                E.Cast(target, Packets);
            }
        }

        public static void InterrupterOnOnPossibleToInterrupt(
            Obj_AI_Hero sender, 
            Interrupter2.InterruptableTargetEventArgs args)
        {
            var unit = sender;
            var spell = args;

            if (Menu.Item("eInterrupt").GetValue<bool>() == false || spell.DangerLevel != Interrupter2.DangerLevel.High)
            {
                return;
            }

            if (!unit.IsValidTarget(E.Range))
            {
                return;
            }

            if (!E.IsReady())
            {
                return;
            }

            E.Cast(unit, Packets);
        }

        public static void OrbwalkingOnBeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target.IsValid<Obj_AI_Minion>()
                && (Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.Mixed
                    || Orbwalker.ActiveMode == Orbwalking.OrbwalkingMode.LastHit)
                && !Menu.Item("AttackMinions").GetValue<bool>())
            {
                if (ObjectManager.Player.CountAlliesInRange(1200) != 0)
                {
                    args.Process = false;
                }
            }

            if (!args.Target.IsValid<Obj_AI_Hero>() || Menu.Item("AttackChampions").GetValue<bool>())
            {
                return;
            }

            if (ObjectManager.Player.CountAlliesInRange(1200) != 0)
            {
                args.Process = false;
            }
        }

        #endregion
    }
}