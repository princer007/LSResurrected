using System;
using LeagueSharp;
using LeagueSharp.Common;
using System.Collections.Generic;
using SharpDX;
using Color = System.Drawing.Color;

namespace RoyalAsheHelper
{
    class Program
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static readonly string champName = "Ashe";
        private static Spell Q, W, E, R;
        private static bool hasQ = false;
        private static Orbwalking.Orbwalker SOW;
        private static Menu menu;
        private const double WAngle = 57.5 * Math.PI / 180;
        
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (player.ChampionName != champName) return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);//57.5º - 2000
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            W.SetSkillshot(0.5f, (float)WAngle, 2000f, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.3f, 250f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.AfterAttack += AfterAttack;
            Orbwalking.BeforeAttack += BeforeAttack;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("RoyalAsheHelper loaded!");
        }

        static void AfterAttack(AttackableUnit unit, AttackableUnit target)
        {
            if (menu.Item("exploit").GetValue<bool>() && menu.Item("UseQ").GetValue<bool>())
                foreach (BuffInstance buff in player.Buffs)
                    if (buff.Name == "FrostShot") Q.Cast();
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (menu.Item("UseQ").GetValue<bool>())
                if (args.Target.Type == GameObjectType.obj_AI_Hero)
                {
                    foreach (BuffInstance buff in player.Buffs)
                        if (buff.Name == "FrostShot") return;
                    Q.Cast();
                }
                else
                {
                    foreach (BuffInstance buff in player.Buffs)
                        if (buff.Name == "FrostShot") Q.Cast();
                    
                }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && menu.SubMenu("misc").Item("antigapcloser").GetValue<bool>() && Vector3.Distance(gapcloser.Sender.Position, player.Position) < 1000)
                R.Cast(gapcloser.End, true);
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() &&
                Vector3.Distance(player.Position, unit.Position) < 1500 &&
                menu.SubMenu("misc").Item("interrupt").GetValue<bool>() &&
                spell.DangerLevel >= InterruptableDangerLevel.Medium &&
                R.GetPrediction(unit).Hitchance >= HitChance.High)
                R.Cast(unit.Position, true);
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            // Harass
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();

            // Laneclear
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.LaneClear)
                Laneclear();
        }

        static void Laneclear()
        {
            List<Obj_AI_Base> minions = MinionManager.GetMinions(player.Position, W.Range);
            MinionManager.FarmLocation WPos = W.GetLineFarmLocation(minions);
            if (menu.SubMenu("Laneclear").Item("UseW").GetValue<bool>() && WPos.MinionsHit >= 3) W.Cast(WPos.Position, true);
        }

        static void Combo()
        {
            bool useW = W.IsReady() && menu.SubMenu("combo").Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && menu.SubMenu("combo").Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            Obj_AI_Hero targetR = TargetSelector.GetTarget(700, TargetSelector.DamageType.Magical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
            if (useR)
            {
                R.CastIfHitchanceEquals(targetR, HitChance.High);
            }
        }
        
        static void Harass()
        {
            bool useW = W.IsReady() && menu.SubMenu("harass").Item("UseW").GetValue<bool>();
            Obj_AI_Hero targetW = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
        }
        
        static void OnDraw(EventArgs args)
        {
            var WRange = menu.Item("Wrange").GetValue<Circle>();
            var ECircle = menu.Item("Ecircle").GetValue<Circle>();
            var ECircle2 = menu.Item("Ecircle2").GetValue<Circle>();

            if (WRange.Active)
            {
                Render.Circle.DrawCircle(player.Position, W.Range, WRange.Color);
                //Utility.DrawCircle(ObjectManager.Player.Position, W.Range, WRange.Color);
            }

            if (ECircle.Active)
            {
                Render.Circle.DrawCircle(player.Position, 2500 + E.Level * 750, ECircle.Color);
            }
            if (ECircle2.Active)
            {
                Render.Circle.DrawCircle(player.Position, 2500 + E.Level * 750, ECircle2.Color, 1, true);
            }
        }

        static void LoadMenu()
        {
            // Initialize the menu
            menu = new Menu("Royal Ashe Helper", champName, true);

            // Target selector
            Menu targetSelector = new Menu("Target Selector", "ts");
            TargetSelector.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            // Orbwalker
            Menu orbwalker = new Menu("Orbwalker", "orbwalker");
            SOW = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("UseR", "Use R").SetValue(true));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseW", "Use W").SetValue(true));

            Menu laneclear = new Menu("Laneclear", "laneclear");
            menu.AddSubMenu(laneclear);
            laneclear.AddItem(new MenuItem("UseW", "Use W for laneclear").SetValue(true));

            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            misc.AddItem(new MenuItem("exploit", "Q exploit").SetValue(false));
            //misc.AddItem(new MenuItem("interruptLevel", "Interrupt only with danger level").SetValue<InterruptableDangerLevel>(InterruptableDangerLevel.Medium));
            misc.AddItem(new MenuItem("antigapcloser", "Anti-Gapscloser").SetValue(true));

            var drawings = new Menu("Drawings", "Drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("Wrange", "W Range").SetValue(new Circle(true, Color.Cyan)));
            drawings.AddItem(new MenuItem("Ecircle", "E Range").SetValue(new Circle(true, Color.ForestGreen)));
            drawings.AddItem(new MenuItem("Ecircle2", "E Range (minimap)").SetValue(new Circle(true, Color.Cyan)));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
