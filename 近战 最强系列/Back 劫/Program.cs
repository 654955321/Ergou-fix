#region
/*
* Credits to:
 * Kortatu(thx for great common lib)
 * Legacy( i got orianna farm logic and some cool examples from your wip zed) 
 * Kurisu (ult on dangerous)
 * Pingo(for finding me proper commands whenever i ask :)
 * Andre(for answerin me whenever i ask him a question ^^ :)
 * xQx assasin target selector
 */
using System;
using System.Collections.Generic;
using System.Linq;
using LeagueSharp;
using LeagueSharp.Common;
using System.Threading.Tasks;
using System.Text;
using SharpDX;
using Color = System.Drawing.Color;

#endregion

namespace Zed
{
    class Program
    {
        private const string ChampionName = "Zed";
        private static List<Spell> SpellList = new List<Spell>();
        private static Spell _q, _w, _e, _r;
        private static Orbwalking.Orbwalker _orbwalker;
        private static Menu _config;
        public static Menu TargetSelectorMenu;
        private static Obj_AI_Hero _player;
        private static SpellSlot _igniteSlot;
        private static Items.Item _tiamat, _hydra, _blade, _bilge, _rand, _lotis, _youmuu;
        private static Vector3 linepos;
        private static Vector3 castpos;
        private static int clockon;
        private static int countults;
        private static int countdanger;
        private static int ticktock;
        private static float hppi;
        private static Vector3 rpos;
        private static int shadowdelay = 0;
        private static int delayw = 500;

        private static void Main(string[] args)
        {
            CustomEvents.Game.OnGameLoad += Game_OnGameLoad;
        }

        private static void Game_OnGameLoad(EventArgs args)
        {
            try
            {
                _player = ObjectManager.Player;
                if (ObjectManager.Player.BaseSkinName != ChampionName) return;
                _q = new Spell(SpellSlot.Q, 900f);
                _w = new Spell(SpellSlot.W, 550f);
                _e = new Spell(SpellSlot.E, 270f);
                _r = new Spell(SpellSlot.R, 650f);

                _q.SetSkillshot(0.25f, 50f, 1700f, false, SkillshotType.SkillshotLine);

                _bilge = new Items.Item(3144, 475f);
                _blade = new Items.Item(3153, 425f);
                _hydra = new Items.Item(3074, 250f);
                _tiamat = new Items.Item(3077, 250f);
                _rand = new Items.Item(3143, 490f);
                _lotis = new Items.Item(3190, 590f);
                _youmuu = new Items.Item(3142, 10);
                _igniteSlot = _player.GetSpellSlot("SummonerDot");

                var enemy = from hero in ObjectManager.Get<Obj_AI_Hero>()
                            where hero.IsEnemy == true
                            select hero;
                // Just menu things test
                _config = new Menu("花边汉化-zed 回归", "Ze-D Is Back", true);

                TargetSelectorMenu = new Menu("目标 选择", "Target Selector");
                TargetSelector.AddToMenu(TargetSelectorMenu);
                _config.AddSubMenu(TargetSelectorMenu);

                _config.AddSubMenu(new Menu("走 砍", "Orbwalking"));
                _orbwalker = new Orbwalking.Orbwalker(_config.SubMenu("Orbwalking"));

                //Combo
                _config.AddSubMenu(new Menu("连 招", "Combo"));
                _config.SubMenu("Combo").AddItem(new MenuItem("UseWC", "使用 W (顺带突进)")).SetValue(true);
                _config.SubMenu("Combo").AddItem(new MenuItem("UseIgnitecombo", "使用 点燃(击杀)")).SetValue(true);
                _config.SubMenu("Combo").AddItem(new MenuItem("ActiveCombo", "开启 连招!").SetValue(new KeyBind(32, KeyBindType.Press)));
                _config.SubMenu("Combo")
                    .AddItem(new MenuItem("TheLine", "线性 连招").SetValue(new KeyBind("T".ToCharArray()[0], KeyBindType.Press)));
                _config.SubMenu("Combo").AddItem(new MenuItem("RbackC", "连招 R 返回")).SetValue(false);
                _config.SubMenu("Combo").AddItem(new MenuItem("RbackL", "连招 R2 返回")).SetValue(false);

                //Harass
                _config.AddSubMenu(new Menu("骚 扰", "Harass"));
                _config.SubMenu("Harass").AddItem(new MenuItem("longhar", "远程 Poke (自动))").SetValue(new KeyBind("U".ToCharArray()[0], KeyBindType.Toggle)));
                _config.SubMenu("Harass").AddItem(new MenuItem("UseItemsharass", "使用 提亚马特/九头蛇")).SetValue(true);
                _config.SubMenu("Harass").AddItem(new MenuItem("UseWH", "使用 W")).SetValue(true);
                _config.SubMenu("Harass")
                    .AddItem(
                        new MenuItem("ActiveHarass", "骚扰 按键!").SetValue(new KeyBind("C".ToCharArray()[0], KeyBindType.Press)));

                //items
                _config.AddSubMenu(new Menu("物品 使用", "items"));
                _config.SubMenu("items").AddSubMenu(new Menu("进 攻", "Offensive"));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Youmuu", "使用 幽梦")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Tiamat", "使用 提亚玛特")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Hydra", "使用 九头蛇")).SetValue(true);
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Bilge", "使用 小弯刀")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BilgeEnemyhp", "敌人 Hp < %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Bilgemyhp", "自己 Hp < %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").SubMenu("Offensive").AddItem(new MenuItem("Blade", "使用 破败")).SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("BladeEnemyhp", "敌人 HP < %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items")
                    .SubMenu("Offensive")
                    .AddItem(new MenuItem("Blademyhp", "自己 Hp < %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("items").AddSubMenu(new Menu("防 守", "Deffensive"));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omen", "使用 兰顿"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("Omenenemys", "使用兰顿丨敌人数 >").SetValue(new Slider(2, 1, 5)));
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotis", "使用 鸟盾"))
                    .SetValue(true);
                _config.SubMenu("items")
                    .SubMenu("Deffensive")
                    .AddItem(new MenuItem("lotisminhp", "使用鸟盾丨友军 Hp < ").SetValue(new Slider(35, 1, 100)));

                //Farm
                _config.AddSubMenu(new Menu("打 钱", "Farm"));
                _config.SubMenu("Farm").AddSubMenu(new Menu("清 线", "LaneFarm"));
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("UseItemslane", "使用 提亚玛特/九头蛇"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseQL", "Q 清线")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("LaneFarm").AddItem(new MenuItem("UseEL", "E 清线")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(new MenuItem("Energylane", "清线丨最低能量 > %").SetValue(new Slider(45, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("LaneFarm")
                    .AddItem(
                        new MenuItem("Activelane", "清线 按键!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                _config.SubMenu("Farm").AddSubMenu(new Menu("补 刀", "LastHit"));
                _config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseQLH", "Q 补刀")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("LastHit").AddItem(new MenuItem("UseELH", "E 补刀")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(new MenuItem("Energylast", "补刀丨最低能量 > %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("LastHit")
                    .AddItem(
                        new MenuItem("ActiveLast", "补刀 按键!").SetValue(new KeyBind("X".ToCharArray()[0], KeyBindType.Press)));

                _config.SubMenu("Farm").AddSubMenu(new Menu("清 野", "Jungle"));
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("UseItemsjungle", "使用 提亚玛特/九头蛇"))
                    .SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseQJ", "Q 清野")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseWJ", "W 清野")).SetValue(true);
                _config.SubMenu("Farm").SubMenu("Jungle").AddItem(new MenuItem("UseEJ", "E 清野")).SetValue(true);
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(new MenuItem("Energyjungle", "清野丨最低能量 > %").SetValue(new Slider(85, 1, 100)));
                _config.SubMenu("Farm")
                    .SubMenu("Jungle")
                    .AddItem(
                        new MenuItem("Activejungle", "清野 按键!").SetValue(new KeyBind("V".ToCharArray()[0], KeyBindType.Press)));

                //Misc
                _config.AddSubMenu(new Menu("杂 项", "Misc"));
                _config.SubMenu("Misc").AddItem(new MenuItem("UseIgnitekill", "使用 点燃 击杀")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("UseQM", "使用 Q 击杀")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("UseEM", "使用 E 击杀")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("AutoE", "自动 E")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("rdodge", "R 躲避 危险")).SetValue(true);
                _config.SubMenu("Misc").AddItem(new MenuItem("Huab", "花边汉化-zed回归杀神!"));
                foreach (var e in enemy)
                {
                    SpellDataInst rdata = e.Spellbook.GetSpell(SpellSlot.R);
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(rdata.SData.Name)))
                        _config.SubMenu("Misc").AddItem(new MenuItem("ds" + e.SkinName, rdata.SData.Name)).SetValue(true);
                }


                //Drawings
                _config.AddSubMenu(new Menu("范 围", "Drawings"));
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQ", "Q 范围")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawE", "E 范围")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawQW", "显示 远程 骚扰")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("DrawR", "R 范围")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("shadowd", "显示 影子 位置")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("damagetest", "显示 伤害 文本")).SetValue(true);
                _config.SubMenu("Drawings").AddItem(new MenuItem("CircleLag", "延迟 自由圈").SetValue(true));
                _config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleQuality", "线圈 质量").SetValue(new Slider(100, 100, 10)));
                _config.SubMenu("Drawings")
                    .AddItem(new MenuItem("CircleThickness", "线圈 厚度").SetValue(new Slider(1, 10, 1)));
                _config.AddToMainMenu();
                Game.PrintChat("<font color='#881df2'>Zed by Diabaths & jackisback 涓ㄦ眽鍖朆y鑺辫竟!</font> ");
                Game.PrintChat("<font color='#f2881d'>if you wanna help me to pay my internet bills^^ paypal= bulut@live.co.uk</font>");

                new AssassinManager();
                Drawing.OnDraw += Drawing_OnDraw;
                Game.OnGameUpdate += Game_OnGameUpdate;
                Game.OnWndProc += OnWndProc;
                Obj_AI_Base.OnProcessSpellCast += OnProcessSpell;
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
                Game.PrintChat("Error something went wrong");
            }



        }

        private static void OnWndProc(WndEventArgs args)
        {
            if (args.Msg == 514)
            {
                linepos = Game.CursorPos;

            }
        }
        private static void OnProcessSpell(Obj_AI_Base unit, GameObjectProcessSpellCastEventArgs castedSpell)
        {
            if (unit.Type != GameObjectType.obj_AI_Hero)
                return;
            if (unit.IsEnemy)
            {
                if (_config.Item("rdodge").GetValue<bool>() && _r.IsReady() && UltStage == UltCastStage.First &&
                _config.Item("ds" + unit.SkinName).GetValue<bool>())
                {
                    if (DangerDB.DangerousList.Any(spell => spell.Contains(castedSpell.SData.Name)) && 
                        (unit.Distance(_player.ServerPosition) < 650f || _player.Distance(castedSpell.End) <= 250f))
                    {
                        if (castedSpell.SData.Name == "SyndraR")
                        {
                            clockon = Environment.TickCount + 150;
                            countdanger = countdanger + 1;
                        }
                        else
                        {
                            _r.Cast(unit);
                        }
                    }
                }
            }
 
            if (unit.IsMe && castedSpell.SData.Name == "zedult")
            {
                ticktock = Environment.TickCount + 200;

            }
        }

        private static void Game_OnGameUpdate(EventArgs args)
        {
            if (_config.Item("ActiveCombo").GetValue<KeyBind>().Active)
            {          
                Combo(GetEnemy);
                
            }
            if (_config.Item("TheLine").GetValue<KeyBind>().Active)
            {
                TheLine(GetEnemy);
            }
            if (_config.Item("ActiveHarass").GetValue<KeyBind>().Active)
            {
                Harass(GetEnemy);

            }
            if (_config.Item("Activelane").GetValue<KeyBind>().Active)
            {
                Laneclear();
            }
            if (_config.Item("Activejungle").GetValue<KeyBind>().Active)
            {
                JungleClear();
            }
            if (_config.Item("ActiveLast").GetValue<KeyBind>().Active)
            {
                LastHit();
            }
            if (_config.Item("AutoE").GetValue<bool>())
            {
                CastE();
            }

            if (Environment.TickCount >= clockon && countdanger > countults)
            {
                _r.Cast(GetEnemy);
                countults = countults + 1;
            }
            if (Environment.TickCount <= ticktock)
            {
                foreach (var enemy in
                ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {
                    if (enemy.HasBuff("zedulttargetmark", true))
                    {
                        hppi = enemy.Health;
                    }
                }
            }

            if (LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R)
            {
                Obj_AI_Minion shadow;
                shadow = ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && minion.Name == "Shadow");

                rpos = shadow.ServerPosition;
            }


            _player = ObjectManager.Player;


            KillSteal();

        }

        private static float ComboDamage(Obj_AI_Base enemy)
        {
            var damage = 0d;
            if (_igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                damage += ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite);
            if (Items.HasItem(3077) && Items.CanUseItem(3077))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Tiamat);
            if (Items.HasItem(3074) && Items.CanUseItem(3074))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Hydra);
            if (Items.HasItem(3153) && Items.CanUseItem(3153))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Botrk);
            if (Items.HasItem(3144) && Items.CanUseItem(3144))
                damage += _player.GetItemDamage(enemy, Damage.DamageItems.Bilgewater);
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.Q) * 1.5;
            if (_q.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.W);
            if (_e.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.E);
            if (_r.IsReady())
                damage += _player.GetSpellDamage(enemy, SpellSlot.R);
            damage += (_r.Level*0.15 + 0.05)*
                      (damage - ObjectManager.Player.GetSummonerSpellDamage(enemy, Damage.SummonerSpell.Ignite));

            return (float)damage;
        }

        private static void Combo(Obj_AI_Hero t)
        {
            var target = t;


            if (target != null && _config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                    _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                 if (ComboDamage(target) > target.Health)
                 {
                     _player.Spellbook.CastSpell(_igniteSlot, target);
                 }
            }
            if (target != null && ShadowStage == ShadowCastStage.First && _config.Item("UseWC").GetValue<bool>() &&
                    target.Distance(_player.Position) > 400 && target.Distance(_player.Position) < 1300)
            {
                CastW(target);
            }
            if (target != null && ShadowStage == ShadowCastStage.Second && _config.Item("UseWC").GetValue<bool>() &&
                target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position))
            {
               _w.Cast();
            }

            if (target != null && target.HasBuff("zedulttargetmark", true) && _config.Item("RbackC").GetValue<bool>() &&
                UltStage == UltCastStage.Second &&
                target.Health < (hppi * (_r.Level * 0.15 + 0.05) - 3 * ((ObjectManager.Player.Level - 1) * 4 + 14)))
            {
                _r.Cast();
            }

            UseItemes(target);
            CastQ(target);
            CastE();
            
        }

        private static void TheLine(Obj_AI_Hero t)
        {
            var target = t;

            if (target == null)
            {
                _player.IssueOrder(GameObjectOrder.MoveTo, Game.CursorPos);
            }
            else
            {
                _player.IssueOrder(GameObjectOrder.AttackUnit, target);
            }

            if ((linepos.X == 0 && linepos.Y == 0) || !_r.IsReady() || target.Distance(_player.Position) >= 640)
            {
                return;
            }

            _r.Cast(target);

            if (target != null && ShadowStage == ShadowCastStage.First &&  UltStage == UltCastStage.Second)
            {
                UseItemes(target);
                
                if (LastCastedSpell.LastCastPacketSent.Slot != SpellSlot.W)
                {
              
                    var m = (double)((linepos.Y - target.ServerPosition.Y) / (linepos.X - target.ServerPosition.X));
                    var angle = (double)Math.Atan(m);

                    if (linepos.X > target.ServerPosition.X)
                    {
                        castpos.X = target.ServerPosition.X + 550 * (float)Math.Cos(angle);
                        castpos.Y = target.ServerPosition.Y + 550 * (float)Math.Sin(angle);
                    }
                    else
                    {
                        castpos.X = target.ServerPosition.X - 550 * (float)Math.Cos(angle);
                        castpos.Y = target.ServerPosition.Y - 550 * (float)Math.Sin(angle);
                    }

                    castpos.Z = target.ServerPosition.Z;

                    _w.Cast(castpos);
                    CastE();
                    _q.Cast(target.Position);
                    
                    
                    if (target != null && _config.Item("UseIgnitecombo").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                            _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
                    {
                        _player.Spellbook.CastSpell(_igniteSlot, target);
                    }
                
                }
            }

            if (target != null && WShadow != null && UltStage == UltCastStage.Second && target.Distance(_player.Position) > 250 && (target.Distance(WShadow.ServerPosition) < target.Distance(_player.Position)))
            {
                _w.Cast();
            }


            if (target != null && target.HasBuff("zedulttargetmark", true) && _config.Item("RbackL").GetValue<bool>() && target.Health <
              (hppi * (_r.Level * 0.15 + 0.05) + 50 + 3*((ObjectManager.Player.Level-1)*4+14)))
            {
                _r.Cast();
            }


        }

        private static void Harass(Obj_AI_Hero t)
        {
            var target = t;

            var useItemsH = _config.Item("UseItemsharass").GetValue<bool>();

            if (target.IsValidTarget() && _config.Item("longhar").GetValue<KeyBind>().Active && _w.IsReady() && _q.IsReady() && ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost && target.Distance(_player.Position) > 850 &&
                target.Distance(_player.Position) < 1400)
            {
                CastW(target);
                CastQ(target);
            }

            if (target.IsValidTarget() && (ShadowStage != ShadowCastStage.First || !(_config.Item("UseWH").GetValue<bool>())) && _q.IsReady() &&
                           (target.Distance(_player.Position) <= 900 || target.Distance(WShadow.ServerPosition) <= 900))
            {
                CastQ(target);
            }

            if (target.IsValidTarget() && _w.IsReady() && _q.IsReady() && _config.Item("UseWH").GetValue<bool>() &&
                ObjectManager.Player.Mana >
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.Q).ManaCost +
                ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).ManaCost && target.Distance(_player.Position) < 850)
            {
                CastW(target);
            }
            
            CastE();
         
            if (useItemsH && _tiamat.IsReady() && target.Distance(_player.Position) < _tiamat.Range)
            {
                _tiamat.Cast();
            }
            if (useItemsH && _hydra.IsReady() && target.Distance(_player.Position) < _hydra.Range)
            {
                _hydra.Cast();
            }
            
        }

        private static void Laneclear()
        {
            var allMinionsQ = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range);
            var allMinionsE = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _e.Range);
            var mymana = (_player.Mana >= (_player.MaxMana*_config.Item("Energylane").GetValue<Slider>().Value)/100);

            var useItemsl = _config.Item("UseItemslane").GetValue<bool>();
            var useQl = _config.Item("UseQL").GetValue<bool>();
            var useEl = _config.Item("UseEL").GetValue<bool>();
            if (_q.IsReady() && useQl && mymana)
            {
                var fl2 = _q.GetLineFarmLocation(allMinionsQ, _q.Width);

                if (fl2.MinionsHit >= 3)
                {
                    _q.Cast(fl2.Position);
                }
                else
                    foreach (var minion in allMinionsQ)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                            _q.Cast(minion);
            }

            if (_e.IsReady() && useEl && mymana)
            {
                if (allMinionsE.Count > 2)
                {
                    _e.Cast();
                }
                else
                    foreach (var minion in allMinionsE)
                        if (!Orbwalking.InAutoAttackRange(minion) &&
                            minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.E))
                            _e.Cast();
            }

            if (useItemsl && _tiamat.IsReady() && allMinionsE.Count > 2)
            {
                _tiamat.Cast();
            }
            if (useItemsl && _hydra.IsReady() && allMinionsE.Count > 2)
            {
                _hydra.Cast();
            }
        }

        private static void LastHit()
        {
            var allMinions = MinionManager.GetMinions(ObjectManager.Player.ServerPosition, _q.Range, MinionTypes.All);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * _config.Item("Energylast").GetValue<Slider>().Value) / 100);
            var useQ = _config.Item("UseQLH").GetValue<bool>();
            var useE = _config.Item("UseELH").GetValue<bool>();
            foreach (var minion in allMinions)
            {
                if (mymana && useQ && _q.IsReady() && _player.Distance(minion.ServerPosition) < _q.Range &&
                    minion.Health < 0.75 * _player.GetSpellDamage(minion, SpellSlot.Q))
                {
                    _q.Cast(minion);
                }

                if (mymana && _e.IsReady() && useE && _player.Distance(minion.ServerPosition) < _e.Range &&
                    minion.Health < 0.95 * _player.GetSpellDamage(minion, SpellSlot.E))
                {
                    _e.Cast();
                }
            }
        }

        private static void JungleClear()
        {
            var mobs = MinionManager.GetMinions(_player.ServerPosition, _q.Range,
                MinionTypes.All,
                MinionTeam.Neutral, MinionOrderTypes.MaxHealth);
            var mymana = (_player.Mana >=
                          (_player.MaxMana * _config.Item("Energyjungle").GetValue<Slider>().Value) / 100);
            var useItemsJ = _config.Item("UseItemsjungle").GetValue<bool>();
            var useQ = _config.Item("UseQJ").GetValue<bool>();
            var useW = _config.Item("UseWJ").GetValue<bool>();
            var useE = _config.Item("UseEJ").GetValue<bool>();

            if (mobs.Count > 0)
            {
                var mob = mobs[0];
                if (mymana && _w.IsReady() && useW && _player.Distance(mob.ServerPosition) < _q.Range)
                {
                    _w.Cast(mob.Position);
                }
                if (mymana && useQ && _q.IsReady() && _player.Distance(mob.ServerPosition) < _q.Range)
                {
                    CastQ(mob);
                }
                if (mymana && _e.IsReady() && useE && _player.Distance(mob.ServerPosition) < _e.Range)
                {
                    _e.Cast();
                }

                if (useItemsJ && _tiamat.IsReady() && _player.Distance(mob.ServerPosition) < _tiamat.Range)
                {
                    _tiamat.Cast();
                }
                if (useItemsJ && _hydra.IsReady() && _player.Distance(mob.ServerPosition) < _hydra.Range)
                {
                    _hydra.Cast();
                }
            }

        }
        static Obj_AI_Hero GetEnemy
        {
            get
            {
                var assassinRange = TargetSelectorMenu.Item("AssassinSearchRange").GetValue<Slider>().Value;

                var vEnemy = ObjectManager.Get<Obj_AI_Hero>()
                    .Where(
                        enemy =>
                            enemy.Team != ObjectManager.Player.Team && !enemy.IsDead && enemy.IsVisible &&
                            TargetSelectorMenu.Item("Assassin" + enemy.ChampionName) != null &&
                            TargetSelectorMenu.Item("Assassin" + enemy.ChampionName).GetValue<bool>() &&
                            ObjectManager.Player.Distance(enemy.ServerPosition) < assassinRange);

                if (TargetSelectorMenu.Item("AssassinSelectOption").GetValue<StringList>().SelectedIndex == 1)
                {
                    vEnemy = (from vEn in vEnemy select vEn).OrderByDescending(vEn => vEn.MaxHealth);
                }

                Obj_AI_Hero[] objAiHeroes = vEnemy as Obj_AI_Hero[] ?? vEnemy.ToArray();

                Obj_AI_Hero t = !objAiHeroes.Any()
                    ? TargetSelector.GetTarget(1400, TargetSelector.DamageType.Magical)
                    : objAiHeroes[0];

                return t;

            }

        }

        private static void UseItemes(Obj_AI_Hero target)
        {
            var iBilge = _config.Item("Bilge").GetValue<bool>();
            var iBilgeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BilgeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBilgemyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Bilgemyhp").GetValue<Slider>().Value) / 100);
            var iBlade = _config.Item("Blade").GetValue<bool>();
            var iBladeEnemyhp = target.Health <=
                                (target.MaxHealth * (_config.Item("BladeEnemyhp").GetValue<Slider>().Value) / 100);
            var iBlademyhp = _player.Health <=
                             (_player.MaxHealth * (_config.Item("Blademyhp").GetValue<Slider>().Value) / 100);
            var iOmen = _config.Item("Omen").GetValue<bool>();
            var iOmenenemys = ObjectManager.Get<Obj_AI_Hero>().Count(hero => hero.IsValidTarget(450)) >=
                              _config.Item("Omenenemys").GetValue<Slider>().Value;
            var iTiamat = _config.Item("Tiamat").GetValue<bool>();
            var iHydra = _config.Item("Hydra").GetValue<bool>();
            var ilotis = _config.Item("lotis").GetValue<bool>();
            var iYoumuu = _config.Item("Youmuu").GetValue<bool>();
            //var ihp = _config.Item("Hppotion").GetValue<bool>();
            // var ihpuse = _player.Health <= (_player.MaxHealth * (_config.Item("Hppotionuse").GetValue<Slider>().Value) / 100);
            //var imp = _config.Item("Mppotion").GetValue<bool>();
            //var impuse = _player.Health <= (_player.MaxHealth * (_config.Item("Mppotionuse").GetValue<Slider>().Value) / 100);

            if (_player.Distance(target.ServerPosition) <= 450 && iBilge && (iBilgeEnemyhp || iBilgemyhp) && _bilge.IsReady())
            {
                _bilge.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 450 && iBlade && (iBladeEnemyhp || iBlademyhp) && _blade.IsReady())
            {
                _blade.Cast(target);

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iTiamat && _tiamat.IsReady())
            {
                _tiamat.Cast();

            }
            if (_player.Distance(target.ServerPosition) <= 300 && iHydra && _hydra.IsReady())
            {
                _hydra.Cast();

            }
            if (iOmenenemys && iOmen && _rand.IsReady())
            {
                _rand.Cast();

            }
            if (ilotis)
            {
                foreach (var hero in ObjectManager.Get<Obj_AI_Hero>().Where(hero => hero.IsAlly || hero.IsMe))
                {
                    if (hero.Health <= (hero.MaxHealth * (_config.Item("lotisminhp").GetValue<Slider>().Value) / 100) &&
                        hero.Distance(_player.ServerPosition) <= _lotis.Range && _lotis.IsReady())
                        _lotis.Cast();
                }
            }
            if (_player.Distance(target.ServerPosition) <= 350 && iYoumuu && _youmuu.IsReady())
            {
                _youmuu.Cast();

            }
        }

        private static Obj_AI_Minion WShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition != rpos) && minion.Name == "Shadow");
            }
        }
        private static Obj_AI_Minion RShadow
        {
            get
            {
                return
                    ObjectManager.Get<Obj_AI_Minion>()
                        .FirstOrDefault(minion => minion.IsVisible && minion.IsAlly && (minion.ServerPosition == rpos) && minion.Name == "Shadow");
            }
        }

        private static UltCastStage UltStage
        {
            get
            {
                if (!_r.IsReady()) return UltCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.R).Name == "zedult"
                    ? UltCastStage.First
                    : UltCastStage.Second);
            }
        }


        private static ShadowCastStage ShadowStage
        {
            get
            {
                if (!_w.IsReady()) return ShadowCastStage.Cooldown;

                return (ObjectManager.Player.Spellbook.GetSpell(SpellSlot.W).Name == "ZedShadowDash"
                    ? ShadowCastStage.First
                    : ShadowCastStage.Second);
            }
        }

        private static void CastW(Obj_AI_Base target)
        {
            if (delayw >= Environment.TickCount - shadowdelay || ShadowStage != ShadowCastStage.First || 
                ( target.HasBuff("zedulttargetmark", true) && LastCastedSpell.LastCastPacketSent.Slot == SpellSlot.R && UltStage == UltCastStage.Cooldown))
                return;
         
            _w.Cast(target.Position, true);
            shadowdelay = Environment.TickCount;

        }

        private static void CastQ(Obj_AI_Base target)
        {
            if (!_q.IsReady()) return;
            if (WShadow != null && target.Distance(WShadow.ServerPosition) <= 900)
            {
                
                _q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                _q.Cast(target);
              
            }
            else
            {
                _q.UpdateSourcePosition(ObjectManager.Player.ServerPosition, ObjectManager.Player.ServerPosition);
                _q.Cast(target);
               

            }
                

        }
        private static void CastE()
        {
            if (!_e.IsReady()) return;
            if (ObjectManager.Get<Obj_AI_Hero>()
                .Count(
                    hero =>
                        hero.IsValidTarget() &&
                        (hero.Distance(ObjectManager.Player.ServerPosition) <= _e.Range ||
                         (WShadow != null && hero.Distance(WShadow.ServerPosition) <= _e.Range))) > 0)
                _e.Cast();
        }

        internal enum UltCastStage
        {
            First,
            Second,
            Cooldown
        }

        internal enum ShadowCastStage
        {
            First,
            Second,
            Cooldown
        }

        private static void KillSteal()
        {
            var target = TargetSelector.GetTarget(2000, TargetSelector.DamageType.Physical);
            var igniteDmg = _player.GetSummonerSpellDamage(target, Damage.SummonerSpell.Ignite);
            if (target.IsValidTarget() && _config.Item("UseIgnitekill").GetValue<bool>() && _igniteSlot != SpellSlot.Unknown &&
                _player.Spellbook.CanUseSpell(_igniteSlot) == SpellState.Ready)
            {
                if (igniteDmg > target.Health && _player.Distance(target.ServerPosition) <= 600)
                {
                    _player.Spellbook.CastSpell(_igniteSlot, target);
                }
            }
            if (target.IsValidTarget() && _q.IsReady() && _config.Item("UseQM").GetValue<bool>() && _q.GetDamage(target) > target.Health)
            {
                if (_player.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.Cast(target);
                }
                else if (WShadow != null && WShadow.Distance(target.ServerPosition) <= _q.Range)
                {
                    _q.UpdateSourcePosition(WShadow.ServerPosition, WShadow.ServerPosition);
                    _q.Cast(target);
                }
            }
            if (_e.IsReady() && _config.Item("UseEM").GetValue<bool>())
            {
                var t = TargetSelector.GetTarget(_e.Range, TargetSelector.DamageType.Physical);
                if (_e.GetDamage(t) > t.Health && (_player.Distance(t.ServerPosition) <= _e.Range || WShadow.Distance(t.ServerPosition) <= _e.Range))
                {
                    _e.Cast();
                }
            }
        }

        private static void Drawing_OnDraw(EventArgs args)
        {
            if (RShadow != null)
            {
                Render.Circle.DrawCircle(RShadow.ServerPosition, RShadow.BoundingRadius * 2, Color.Blue);
            }

            if (_config.Item("TheLine").GetValue<KeyBind>().Active)
            {
                Render.Circle.DrawCircle(linepos, 75, Color.Blue);
                Render.Circle.DrawCircle(castpos, 75, Color.Red);
            }
            if (_config.Item("shadowd").GetValue<bool>())
            {
                if (WShadow != null)
                {
                    if (ShadowStage == ShadowCastStage.Cooldown)
                    {
                        Render.Circle.DrawCircle(WShadow.ServerPosition, WShadow.BoundingRadius * 1.5f, Color.Red);
                    }
                    else if (WShadow != null && ShadowStage == ShadowCastStage.Second)
                    {
                        Render.Circle.DrawCircle(WShadow.ServerPosition, WShadow.BoundingRadius * 1.5f, Color.Yellow);
                    }
                }
            }
            if (_config.Item("damagetest").GetValue<bool>())
            {
                foreach (
                    var enemyVisible in
                        ObjectManager.Get<Obj_AI_Hero>().Where(enemyVisible => enemyVisible.IsValidTarget()))
                {
                    if (enemyVisible.HasBuff("zedulttargetmark", true) && enemyVisible.Health <
                            (hppi * (_r.Level * 0.15 + 0.05)  - 3 * ((ObjectManager.Player.Level - 1) * 4 + 14)))
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50 ,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] -20, Color.Yellow,
                            "deathmark will kill");
                    }

                    if (ComboDamage(enemyVisible) > enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Red,
                            "Combo=Rekt");
                    }
                    else if (ComboDamage(enemyVisible) + _player.GetAutoAttackDamage(enemyVisible, true) * 2 >
                             enemyVisible.Health)
                    {
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Orange,
                            "Combo+AA=Rekt");
                    }
                    else
                        Drawing.DrawText(Drawing.WorldToScreen(enemyVisible.Position)[0] + 50,
                            Drawing.WorldToScreen(enemyVisible.Position)[1] - 40, Color.Green,
                            "Unkillable");
                }
            }

            if (_config.Item("CircleLag").GetValue<bool>())
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.Blue);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawQW").GetValue<bool>() && _config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.Yellow);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Render.Circle.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.Blue);
                }
            }
            else
            {
                if (_config.Item("DrawQ").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _q.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawE").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _e.Range, System.Drawing.Color.White);
                }
                if (_config.Item("DrawQW").GetValue<bool>() && _config.Item("longhar").GetValue<KeyBind>().Active)
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, 1400, System.Drawing.Color.White);
                }
                if (_config.Item("DrawR").GetValue<bool>())
                {
                    Drawing.DrawCircle(ObjectManager.Player.Position, _r.Range, System.Drawing.Color.White);
                }
            }
        }
    }
}
