using System;
using LeagueSharp;
using LeagueSharp.Common;
using SharpDX;

namespace RoyalAsheHelper
{
    class Program
    {
        private static readonly Obj_AI_Hero player = ObjectManager.Player;
        private static readonly string champName = "Ashe";
        private static Spell Q, W, R;
        private static bool hasQ = false;
        private static Orbwalking.Orbwalker SOW;
        private static Menu menu;
        private const double WAngle = 57.5 * Math.PI / 180;
        
        static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }
        private static void Game_OnGameLoad(EventArgs args)
        {
            if (player.ChampionName != champName) return;
            Q = new Spell(SpellSlot.Q);
            W = new Spell(SpellSlot.W, 1200);//57.5º - 2000
            R = new Spell(SpellSlot.R);
            W.SetSkillshot(0.5f, (float)WAngle, 2000f, false, SkillshotType.SkillshotCone);
            R.SetSkillshot(0.3f, 250f, 1600f, false, SkillshotType.SkillshotLine);
            LoadMenu();
            Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Game.PrintChat("RoyalAsheHelper loaded!");
        }
        private static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {
            if (R.IsReady() && menu.SubMenu("misc").Item("antigapcloser").GetValue<bool>() && Vector3.Distance(gapcloser.Sender.Position, player.Position) < 1000)
            {
                R.Cast(gapcloser.End, true);
            }
        }
        /// <summary>
        /// Interruptor
        /// </summary>
        /// <param name="unit">Unit that causing interruptable spell</param>
        /// <param name="spell">Spell that can be interrupted</param>
        private static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (R.IsReady() &&
                Vector3.Distance(player.Position, unit.Position) < 1000 &&
                menu.SubMenu("misc").Item("interrupt").GetValue<bool>() &&
                spell.DangerLevel >= InterruptableDangerLevel.Medium)
            {
                R.Cast(unit.Position, true);
            }
        }
        private static void Game_OnGameUpdate(EventArgs args)
        {
            // Combo
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            // Harass
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();
        }
        private static void Combo()
        {
            bool useW = W.IsReady() && menu.SubMenu("combo").Item("UseW").GetValue<bool>();
            bool useR = R.IsReady() && menu.SubMenu("combo").Item("UseR").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            Obj_AI_Hero targetR = SimpleTs.GetTarget(700, SimpleTs.DamageType.Magical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
            if (useR)
            {
                R.CastIfHitchanceEquals(targetR, HitChance.High);
            }
        }
        private static void Harass()
        {
            bool useW = W.IsReady() && menu.SubMenu("harass").Item("UseW").GetValue<bool>();
            Obj_AI_Hero targetW = SimpleTs.GetTarget(W.Range, SimpleTs.DamageType.Physical);
            if (useW)
            {
                W.CastIfHitchanceEquals(targetW, HitChance.Medium);
            }
        }
        private static void OnSendPacket(GamePacketEventArgs args)
        {
            if (!menu.SubMenu("combo").Item("UseQ").GetValue<bool>()) return;
            if (args.PacketData[0] == Packet.C2S.Move.Header && Packet.C2S.Move.Decoded(args.PacketData).SourceNetworkId == player.NetworkId && Packet.C2S.Move.Decoded(args.PacketData).MoveType == 3)
            {
                bool heroFound;
                foreach (BuffInstance buff in player.Buffs)
                    if (buff.Name == "FrostShot") hasQ = true;
                heroFound = false;
                foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                    if (hero.NetworkId == Packet.C2S.Move.Decoded(args.PacketData).TargetNetworkId)
                        heroFound = true;
                if (heroFound)
                {
                   if (!hasQ) Q.Cast();
                   hasQ = true;
                }
                else
                {
                    if (hasQ) Q.Cast();
                    hasQ = false;
                }
            }
        }
        private static void LoadMenu()
        {
            // Initialize the menu
            menu = new Menu(champName, champName, true);

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
            combo.AddItem(new MenuItem("UseQ", "Use Q").SetValue(true));
            combo.AddItem(new MenuItem("UseW", "Use W").SetValue(true));
            combo.AddItem(new MenuItem("UseR", "Use R").SetValue(true));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseW", "Use W").SetValue(true));

            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            //misc.AddItem(new MenuItem("interruptLevel", "Interrupt only with danger level").SetValue<InterruptableDangerLevel>(InterruptableDangerLevel.Medium));
            misc.AddItem(new MenuItem("antigapcloser", "Anti-Gapscloser").SetValue(true));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
