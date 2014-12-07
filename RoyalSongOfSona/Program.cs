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

            Q = new Spell(SpellSlot.Q, 850);
            W = new Spell(SpellSlot.W, 1000);
            E = new Spell(SpellSlot.E, 350);
            R = new Spell(SpellSlot.R, 1000);

            R.SetSkillshot(0.5f, 125, 3000f, false, SkillshotType.SkillshotLine);

            LoadMenu();
            //Game.OnGameSendPacket += OnSendPacket;
            Game.OnGameUpdate += Game_OnGameUpdate;
            //AntiGapcloser.OnEnemyGapcloser += AntiGapcloser_OnEnemyGapcloser;
            Interrupter.OnPossibleToInterrupt += Interrupter_OnPossibleToInterrupt;
            Orbwalking.BeforeAttack += BeforeAttack;
            Game.PrintChat("RoyalSongOfSona loaded!");
        }

        static void BeforeAttack(LeagueSharp.Common.Orbwalking.BeforeAttackEventArgs args)
        {
            if (args.Target.IsMinion && !menu.Item("aa").GetValue<bool>()) args.Process = false;
        }

        static void AntiGapcloser_OnEnemyGapcloser(ActiveGapcloser gapcloser)
        {

        }

        static void Interrupter_OnPossibleToInterrupt(Obj_AI_Base unit, InterruptableSpell spell)
        {
            if (!unit.IsValid || unit.IsDead || !unit.IsTargetable || unit.IsStunned) return;
            if (R.IsReady() && R.InRange(unit.Position) && spell.DangerLevel >= InterruptableDangerLevel.High)
            {
                R.Cast(unit.Position, true);
                return;
            }
            else
            {
                if (!menu.Item("exhaust").GetValue<bool>()) return;
				if(unit.Distance(player.Position) > 600) return;
                if (player.GetSpellSlot("SummonerExhaust") != null && player.SummonerSpellbook.CanUseSpell(player.GetSpellSlot("SummonerExhaust")) == SpellState.Ready)
                    player.SummonerSpellbook.CastSpell(player.GetSpellSlot("SummonerExhaust"), unit);
                if ((W.IsReady() && GetPassiveCount() == 2) || (Utility.HasBuff(player, "sonapassiveattack") && player.LastCastedSpellName() == "SonaW") || (Utility.HasBuff(player, "sonapassiveattack") && W.IsReady()))
                {
                    if (W.IsReady()) W.Cast();
                    player.IssueOrder(GameObjectOrder.AttackUnit, unit);
                }
            }
        }

        static int GetPassiveCount()
        {
            foreach (BuffInstance buff in player.Buffs)
                if (buff.Name == "sonapassivecount") return buff.Count;
            return 0;
        }

        static void Game_OnGameUpdate(EventArgs args)
        {
            if (menu.Item("panic").GetValue<KeyBind>().Active)
            {
                R.Cast(R.GetPrediction(SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical)).CastPosition, packets);
            }

            // Combo
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Combo)
                Combo();

            // Harass
            if (SOW.ActiveMode == Orbwalking.OrbwalkingMode.Mixed)
                Harass();
        }
        
        static void Combo()
        {
            bool useQ = Q.IsReady() && menu.Item("UseQC").GetValue<bool>();
            bool useW = W.IsReady() && menu.Item("UseWC").GetValue<bool>();
            bool useE = E.IsReady() && menu.Item("UseEC").GetValue<bool>();
            bool useR = R.IsReady() && menu.Item("UseRC").GetValue<bool>();
            Obj_AI_Hero targetQ = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            Obj_AI_Hero targetR = SimpleTs.GetTarget(R.Range, SimpleTs.DamageType.Magical);
            foreach (var item in player.InventoryItems)
                if (item.Id == ItemId.Frost_Queens_Claim && player.Spellbook.CanUseSpell((SpellSlot)item.Slot) == SpellState.Ready)
                    item.UseItem(targetQ);

            if (useQ && targetQ != null && Q.InRange(targetQ.Position))
                Q.Cast();

            if (useW)
                UseWSmart(menu.Item("healC").GetValue<Slider>().Value, menu.Item("healN").GetValue<Slider>().Value);

            if (useE)
                UseESmart(SimpleTs.GetTarget(1700, SimpleTs.DamageType.Magical));
            if (useR && targetR != null)
                R.CastIfWillHit(targetR, menu.Item("RN").GetValue<Slider>().Value, packets);
        }

        static void Harass()
        {
            bool useQ = Q.IsReady() && menu.Item("UseQH").GetValue<bool>();
            bool useW = W.IsReady() && menu.Item("UseWH").GetValue<bool>();
            Obj_AI_Hero targetQ = SimpleTs.GetTarget(Q.Range, SimpleTs.DamageType.Magical);
            if (useQ && targetQ != null && Utility.CountEnemysInRange((int)Q.Range) > 1)
                Q.Cast();

            if (useW)
                UseWSmart(menu.Item("healC").GetValue<Slider>().Value, menu.Item("healN").GetValue<Slider>().Value);
        }

        static void UseWSmart(int percent, int count)
        {
            Obj_AI_Hero ally = MostWoundedAllyInRange(W.Range);
            double wHeal = (10 + 20 * W.Level + .2 * player.FlatMagicDamageMod) * (1 + (1 - (player.Health / player.MaxHealth)) / 2);
            int allies = AlliesInRange(W.Range);

            if (allies >= count && (ally.Health / ally.MaxHealth) * 100 <= percent && ally != default(Obj_AI_Hero))
                W.Cast();
            if (allies < 2 && menu.Item("healmC").GetValue<bool>())
                if (menu.Item("healmC2").GetValue<bool>() && player.MaxHealth - player.Health > wHeal)
                    W.Cast();
                else if ((player.Health / player.MaxHealth) * 100 <= percent) W.Cast(); ;
        }

        //Ty DETUKS, copypasted as fuck :P
        public static void UseESmart(Obj_AI_Base target)
        {
            try
            {

                if (target.Path.Length == 0 || !target.IsMoving)
                    return;
                Vector2 nextEnemPath = target.Path[0].To2D();
                var dist = player.Position.To2D().Distance(target.Position.To2D());
                var distToNext = nextEnemPath.Distance(player.Position.To2D());
                if (distToNext <= dist)
                    return;
                var msDif = player.MoveSpeed - target.MoveSpeed;
                if (msDif <= 0 && !Orbwalking.InAutoAttackRange(target))
                    E.Cast();

                var reachIn = dist / msDif;
                if (reachIn > 4)
                    E.Cast();
            }
            catch { }

        }

        static Obj_AI_Hero MostWoundedAllyInRange(float range)
        {
            float lastHealth = 9000f;
            Obj_AI_Hero temp = new Obj_AI_Hero();
            foreach (Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if (hero.IsAlly && !hero.IsMe && player.Distance(hero) <= range && hero.Health < lastHealth)
                {
                    lastHealth = hero.Health;
                    temp = hero;
                }
            return temp;
        }

        static int AlliesInRange(float range)
        {
            int count = 0;
            foreach(Obj_AI_Hero hero in ObjectManager.Get<Obj_AI_Hero>())
                if(hero.IsAlly && !hero.IsMe && player.Distance(hero) <= range) count++;
            return count;
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
            combo.AddItem(new MenuItem("UseEC", "Use E (smart)").SetValue(true));
            combo.AddItem(new MenuItem("UseRC", "Use R").SetValue(true));
            combo.AddItem(new MenuItem("RN", "Ulti if hit").SetValue(new Slider(2, 1, 5)));

            // Harass
            Menu harass = new Menu("Harass", "harass");
            menu.AddSubMenu(harass);
            harass.AddItem(new MenuItem("UseQH", "Use Q").SetValue(true));
            harass.AddItem(new MenuItem("UseWH", "Use W").SetValue(true));

            Menu heal = new Menu("Heal options", "heal");
            menu.AddSubMenu(heal);
            heal.AddItem(new MenuItem("healC", "Heal only when ally with hp < x%").SetValue(new Slider(60, 5, 100)));
            heal.AddItem(new MenuItem("healN", "Heal only when № of allies in range").SetValue(new Slider(1, 0, 4)));
            heal.AddItem(new MenuItem("healmC", "Heal yourself anyway").SetValue(true));
            heal.AddItem(new MenuItem("healmC2", "^ ON: Fill HP | Same as for others :OFF").SetValue(true));

            Menu misc = new Menu("Misc", "misc");
            menu.AddSubMenu(misc);
            misc.AddItem(new MenuItem("interrupt", "Interrupt spells").SetValue(true));
            misc.AddItem(new MenuItem("aa", "AA minions").SetValue(false));
            misc.AddItem(new MenuItem("exhaust", "Exhaust if not possible to inperrupt").SetValue(true));
            misc.AddItem(new MenuItem("packets", "Packet cast").SetValue(true));
            misc.AddItem(new MenuItem("panic", "Panic ult key").SetValue(new KeyBind('T', KeyBindType.Press)));

            // Finalize menu
            menu.AddToMainMenu();
            Console.WriteLine("Menu finalized");
        }
    }
}
