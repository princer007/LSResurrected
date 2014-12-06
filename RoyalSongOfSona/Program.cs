using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RoyalAsheHelper
{
    class Program
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static Spell Q, W, E, R;
        private static Orbwalking.Orbwalker SOW;
        private static Menu menu;
        private static bool packets { get { return menu.Item("packets").GetValue<bool>(); } }
        
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        static void Game_OnGameLoad(EventArgs args)
        {
            if (player.ChampionName != "Sona") return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W);
            E = new Spell(SpellSlot.E);
            R = new Spell(SpellSlot.R);
            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Game.PrintChat("RoyalSongOfSona loaded!");
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() && R.InRange(unit.Position) && spell.DangerLevel >= InterruptableDangerLevel.High)
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
        }
        
        static void Combo()
        {
            bool useW = W.IsReady() && menu.SubMenu("combo").Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && menu.SubMenu("combo").Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);

        }
        
        static void Harass()
        {
            bool useQ = Q.IsReady() && menu.Item("UseQH").GetValue<bool>();
            Obj_AI_Hero targetQ = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            if (useQ)
            {
                Q.CastIfWillHit(targetQ, 2, packets);
            }
        }
                
        static void LoadMenu()
        {
            // Initialize the menu
            menu = new Menu("Royal Song of Sona", "sona", true);

            // Target selector
            Menu targetSelector = new Menu("Target Selector", "ts");
            SimpleTs.AddToMenu(targetSelector);
            menu.AddSubMenu(targetSelector);

            // Orbwalker
            Menu orbwalker = new Menu("Orbwalker", "orbwalker");
            SOW = new Orbwalking.Orbwalker(orbwalker);
            menu.AddSubMenu(orbwalker);

            // Combo
            Menu combo = new Menu("Combo", "combo");
            menu.AddSubMenu(combo);
            combo.AddItem(new MenuItem("UseQC", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseWC", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("UseEC", "Use E").SetValue(true));
            combo.AddItem(new MenuItem("UseRC", "Use R").SetValue(true));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseQH", "Use Q").SetValue(true));

            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            misc.AddItem(new MenuItem("packets", "Packet cast").SetValue(true));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
