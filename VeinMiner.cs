using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace VeinMinerV6
{
    [ApiVersion(2, 1)]
    public class VeinMiner : TerrariaPlugin
    {
        public override string Name => "VeinMiner & Melee Rework";
        public override string Author => "Gemini";
        public override Version Version => new Version(6, 1, 8);

        private DateTime[] _lastMeleeEffect = new DateTime[256];

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData, 10);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike, 10);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            if (args.Handled) return;

            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null || !player.Active) return;

            if ((DateTime.UtcNow - _lastMeleeEffect[player.Index]).TotalMilliseconds < 250) return;
            _lastMeleeEffect[player.Index] = DateTime.UtcNow;

            Item item = player.TPlayer.HeldItem;

            if (item.damage > 0 && item.melee)
            {
                int dustType = GetDustForSword(item.type);
                SpawnDustExplosion(args.Npc.Center, dustType, 4);

                if (item.damage > 60 || IsSpecialSword(item.type))
                {
                    DoHomingEnergy(args.Npc, player, dustType);
                }

                if (item.type == ItemID.NightsEdge)
                {
                    int heal = Main.rand.Next(1, 3);
                    player.TPlayer.statLife += heal;
                    player.TPlayer.HealEffect(heal);
                }
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            // Balik pakai angka 17 biar aman dari error definisi PacketTypes
            if ((int)args.MsgID == 17)
            {
                using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    byte action = reader.ReadByte();
                    short x = reader.ReadInt16();
                    short y = reader.ReadInt16();

                    if (action == 1) 
                    {
                        TSPlayer player = TShock.Players[args.Msg.whoAmI];
                        if (player != null && player.TPlayer.HeldItem.pick > 0)
                        {
                            ITile tile = Main.tile[x, y];
                            // Perbaikan: Pakai active() sebagai fungsi
                            if (tile != null && tile.active() && TileID.Sets.Ore[tile.type])
                            {
                                MassiveMineWithPickup(x, y, tile.type, player);
                            }
                        }
                    }
                }
            }
        }

        private void MassiveMineWithPickup(int x, int y, ushort oreType, TSPlayer player)
        {
            int mined = 0;
            Queue<Point> toMine = new Queue<Point>();
            toMine.Enqueue(new Point(x, y));
            HashSet<Point> done = new HashSet<Point>();
            int itemType = GetItemDrop(oreType);

            while (toMine.Count > 0 && mined < 100)
            {
                Point p = toMine.Dequeue();
                if (done.Contains(p) || p.X < 5 || p.X >= Main.maxTilesX - 5 || p.Y < 5 || p.Y >= Main.maxTilesY - 5) continue;
                done.Add(p);

                ITile tile = Main.tile[p.X, p.Y];
                if (tile != null && tile.active() && tile.type == oreType)
                {
                    mined++;
                    WorldGen.KillTile(p.X, p.Y, false, false, true);
                    // Kirim paket 17 secara manual
                    NetMessage.SendData(17, -1, -1, null, 1, p.X, p.Y, 0, 0);

                    if (itemType > 0) player.GiveItem(itemType, 1);

                    toMine.Enqueue(new Point(p.X + 1, p.Y));
                    toMine.Enqueue(new Point(p.X - 1, p.Y));
                    toMine.Enqueue(new Point(p.X, p.Y + 1));
                    toMine.Enqueue(new Point(p.X, p.Y - 1));
                }
            }
        }

        private void DoHomingEnergy(NPC source, TSPlayer player, int dust)
        {
            NPC target = Main.npc.FirstOrDefault(n => n != null && n.active && !n.friendly && n.whoAmI != source.whoAmI && Vector2.Distance(source.Center, n.Center) < 300);
            
            if (target != null)
            {
                int dmg = (int)(player.TPlayer.HeldItem.damage * 0.4f);
                player.TPlayer.ApplyDamageToNPC(target, dmg, 0, 0, false);
                
                for (int i = 0; i < 8; i++) {
                    Vector2 dustPos = Vector2.Lerp(source.Center, target.Center, i / 8f);
                    int d = Dust.NewDust(dustPos, 1, 1, dust);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].scale = 0.8f;
                }
            }
        }

        private int GetItemDrop(ushort tileType)
        {
            for (int i = 0; i < ItemID.Count; i++) {
                Item item = new Item();
                item.SetDefaults(i);
                if (item.createTile == tileType) return i;
            }
            return 0;
        }

        private int GetDustForSword(int type) {
            switch(type) {
                case ItemID.Muramasa: return 29;
                case ItemID.NightsEdge: return 27;
                case ItemID.TerraBlade: return 107;
                case ItemID.FieryGreatsword: return 6;
                default: return 31;
            }
        }

        private bool IsSpecialSword(int type) {
            int[] special = { ItemID.Muramasa, ItemID.NightsEdge, ItemID.TerraBlade, ItemID.Excalibur };
            return special.Contains(type);
        }

        private void SpawnDustExplosion(Vector2 pos, int type, int count) {
            for (int i = 0; i < count; i++) {
                float vX = (float)(Main.rand.NextDouble() * 4 - 2);
                float vY = (float)(Main.rand.NextDouble() * 4 - 2);
                int d = Dust.NewDust(pos, 4, 4, type, vX, vY);
                Main.dust[d].noGravity = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
            }
            base.Dispose(disposing);
        }
    }
}
