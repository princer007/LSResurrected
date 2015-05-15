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
            R = new Spell(SpellSlot.R, 1000);
            W.SetSkillshot(0.5f, (float)WAngle, 2000f, true, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.3f, 200f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter2.OnInterruptableTarget += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += BeforeAttack;
            Drawing.OnDraw += OnDraw;
            Game.PrintChat("RoyalAsheHelper loaded!");
        }

        static void BeforeAttack(Orbwalking.BeforeAttackEventArgs args)
        {
            if (menu.Item("UseQc").GetValue<bool>() && SOW.ActiveMode == Orbwalking.OrbwalkingMode.Combo || menu.Item("UseQh").GetValue<bool>() && SOW.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                if (args.Target.Type == GameObjectType.obj_AI_Hero)
                {
                    foreach (BuffInstance buff in player.Buffs)
                    {
                        if (buff.Name == "asheqcastready" && buff.Count == 5)
                            Q.Cast();
                    }
                }
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && menu.SubMenu("misc").Item("antigapcloser").GetValue<bool>() && Vector3.Distance(gapcloser.Sender.Position, player.Position) < 1000)
                R.Cast(gapcloser.End, true);
        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Hero sender, Interrupter2.InterruptableTargetEventArgs args)
        {
            if (R.IsReady() &&
                Vector3.Distance(player.Position, sender.Position) < 1500 &&
                menu.SubMenu("misc").Item("interrupt").GetValue<bool>() &&
                args.DangerLevel >= Interrupter2.DangerLevel.High)
            {
                var pred = R.GetPrediction(sender);
                if(pred.Hitchance >= HitChance.High)
                    R.Cast(pred.CastPosition, true);
            }
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (menu.Item("forceR").GetValue<KeyBind>().Active)
            {
                var target = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
                R.CastIfHitchanceEquals(target, HitChance.High);
            }
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
            List<Obj_AI_Base> minions = MinionManager.GetMinions(600, MinionTypes.All, MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            bool useQ = Q.IsReady() && menu.Item("UseQj").GetValue<bool>();
            bool useW = W.IsReady() && menu.Item("UseWj").GetValue<bool>();
            foreach(var minion in minions)
                if (!minion.IsDead)
                {
                    if (useW) W.Cast(minion.Position);
                    foreach (BuffInstance buff in player.Buffs)
                    {
                        if (buff.Name == "asheqcastready" && buff.Count == 5 && useQ)
                            Q.Cast();
                    }
                }
        }

        static void Combo()
        {
            bool useW = W.IsReady() && menu.SubMenu("combo").Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && menu.SubMenu("combo").Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = TargetSelector.GetTarget(W.Range, TargetSelector.DamageType.Physical);
            Obj_AI_Hero targetR = TargetSelector.GetTarget(R.Range, TargetSelector.DamageType.Magical);
            if (useW)
            {
                if(!menu.SubMenu("combo").Item("UseWE").GetValue<bool>() || targetW.Distance3D(player) > 600)
                    W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
            if (useR)
            {
                Console.WriteLine(CorrectCountAlliesInRange(targetR, 250));
                if (CorrectCountAlliesInRange(targetR, 250) >= menu.Item("RSlider").GetValue<Slider>().Value)
					R.CastIfHitchanceEquals(targetR, HitChance.High);
            }
        }
		
		static int CorrectCountAlliesInRange(Obj_AI_Hero unit, int range)
		{
            //Yes it SHOULD count unit as an ally
            int counter = 0;
            foreach (var cUnit in ObjectManager.Get<Obj_AI_Hero>())
            {
                if (cUnit.Team == unit.Team && !cUnit.IsDead && cUnit.Distance3D(unit) <= range)
                    counter++;
            }
            return counter;
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
            var RCircle = menu.Item("Ecircle").GetValue<Circle>();

            if (WRange.Active)
            {
                Render.Circle.DrawCircle(player.Position, W.Range, WRange.Color);
                //Utility.DrawCircle(ObjectManager.Player.Position, W.Range, WRange.Color);
            }

            if (RCircle.Active)
            {
                Render.Circle.DrawCircle(player.Position, R.Range, RCircle.Color);
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
            combo.AddItem(new MenuItem("UseQc", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "Use W").SetValue(true)); ;
            combo.AddItem(new MenuItem("UseWE", "W only if out of AA range").SetValue(false));
            combo.AddItem(new MenuItem("UseR", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("RSlider", "Enemies to ult").SetValue(new Slider(3, 1, 5)));
            combo.AddItem(new MenuItem("forceR", "Insta cast R").SetValue(new KeyBind('T', KeyBindType.Press)));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseQh", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("UseW", "Use W").SetValue(true));

            // Harass
            Menu jc = new Menu("Jungle clear", "jc");
            menu.AddSubMenu(jc);
            jc.AddItem(new MenuItem("UseQj", "Use Q").SetValue(true));
            jc.AddItem(new MenuItem("UseWj", "Use W").SetValue(true));

            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            misc.AddItem(new MenuItem("antigapcloser", "Anti-Gapscloser").SetValue(true));

            var drawings = new Menu("Drawings", "Drawings");
            menu.AddSubMenu(drawings);
            drawings.AddItem(new MenuItem("Wrange", "W Range").SetValue(new Circle(true, Color.Cyan)));
            drawings.AddItem(new MenuItem("Ecircle", "R Range(combo)").SetValue(new Circle(true, Color.ForestGreen)));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
