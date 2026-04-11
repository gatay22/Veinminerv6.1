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
        public override Version Version => new Version(6, 1, 10);

        private DateTime[] _lastMeleeEffect = new DateTime[256];

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            var player = TShock.Players[args.Who];
            if (player != null)
                player.SendMessage("VeinMiner & Melee Rework [TShock 6.1 - .NET 9] Aktif!", Color.Cyan);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null || !player.Active) return;

            // Cooldown 200ms
            if ((DateTime.UtcNow - _lastMeleeEffect[player.Index]).TotalMilliseconds < 200) return;
            _lastMeleeEffect[player.Index] = DateTime.UtcNow;

            Item item = player.TPlayer.HeldItem;
            if (item != null && item.damage > 0 && item.melee)
            {
                int dustType = GetDustForSword(item.type);
                
                // Visual Strike
                SpawnDustExplosion(args.Npc.Center, dustType, 6);

                // Homing Logic
                if (item.damage > 40)
                {
                    DoHomingEnergy(args.Npc, player, dustType);
                }
            }
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID == PacketTypes.TileEdit) // ID 17
            {
                using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    byte action = reader.ReadByte();
                    short x = reader.ReadInt16();
                    short y = reader.ReadInt16();
                    ushort editType = reader.ReadUInt16();

                    if (action == 1) // Dig
                    {
                        TSPlayer player = TShock.Players[args.Msg.whoAmI];
                        if (player == null || !player.Active) return;

                        // Di .NET 9 / TShock 6.1, pastikan koordinat valid sebelum akses tile
                        if (x < 0 || x >= Main.maxTilesX || y < 0 || y >= Main.maxTilesY) return;

                        ITile tile = Main.tile[x, y];
                        if (tile != null && tile.active() && TileID.Sets.Ore[tile.type])
                        {
                            // Jalankan veinminer
                            MassiveMine(x, y, tile.type, player);
                        }
                    }
                }
            }
        }

        private void MassiveMine(int x, int y, ushort oreType, TSPlayer player)
        {
            int mined = 0;
            Queue<Point> nodes = new Queue<Point>();
            nodes.Enqueue(new Point(x, y));
            HashSet<Point> visited = new HashSet<Point>();

            while (nodes.Count > 0 && mined < 100)
            {
                Point p = nodes.Dequeue();
                if (visited.Contains(p) || p.X < 5 || p.X >= Main.maxTilesX - 5 || p.Y < 5 || p.Y >= Main.maxTilesY - 5) continue;
                visited.Add(p);

                ITile tile = Main.tile[p.X, p.Y];
                if (tile.active() && tile.type == oreType)
                {
                    mined++;
                    // Eksekusi hancur blok
                    WorldGen.KillTile(p.X, p.Y, false, false, false);
                    NetMessage.SendData(17, -1, -1, null, 1, p.X, p.Y, 0, 0);

                    nodes.Enqueue(new Point(p.X + 1, p.Y));
                    nodes.Enqueue(new Point(p.X - 1, p.Y));
                    nodes.Enqueue(new Point(p.X, p.Y + 1));
                    nodes.Enqueue(new Point(p.X, p.Y - 1));
                }
            }
        }

        private void DoHomingEnergy(NPC source, TSPlayer player, int dust)
        {
            // Cari target valid terdekat
            var target = Main.npc.FirstOrDefault(n => n != null && n.active && !n.friendly && n.whoAmI != source.whoAmI && Vector2.Distance(source.Center, n.Center) < 350);
            
            if (target != null)
            {
                int dmg = (int)(player.TPlayer.HeldItem.damage * 0.4f);
                player.TPlayer.ApplyDamageToNPC(target, dmg, 0, 0, false);
                
                for (int i = 0; i < 10; i++) {
                    Vector2 dustPos = Vector2.Lerp(source.Center, target.Center, i / 10f);
                    int d = Dust.NewDust(dustPos, 1, 1, dust, 0, 0, 150, default, 1.2f);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0.1f;
                }
            }
        }

        private int GetDustForSword(int type) {
            if (type == ItemID.Muramasa) return 29;
            if (type == ItemID.NightsEdge) return 27;
            if (type == ItemID.TerraBlade) return 107;
            return 31;
        }

        private void SpawnDustExplosion(Vector2 pos, int type, int count) {
            for (int i = 0; i < count; i++) {
                int d = Dust.NewDust(pos, 8, 8, type, Main.rand.Next(-2, 3), Main.rand.Next(-2, 3), 100, default, 1f);
                Main.dust[d].noGravity = true;
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetGreetPlayer.Deregister(this, OnGreetPlayer);
                ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
                ServerApi.Hooks.NpcStrike.Deregister(this, OnNpcStrike);
            }
            base.Dispose(disposing);
        }
    }
}
