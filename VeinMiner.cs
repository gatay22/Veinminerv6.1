using System;
using System.Collections.Generic;
using System.IO;
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
        public override Version Version => new Version(6, 1, 5);

        private Dictionary<int, DateTime> _lastEffectTime = new Dictionary<int, DateTime>();

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
            // Perbaikan: Pakai NpcStrike (case sensitive)
            ServerApi.Hooks.NpcStrike.Register(this, OnNpcStrike);
        }

        private void OnNpcStrike(NpcStrikeEventArgs args)
        {
            TSPlayer player = TShock.Players[args.Player.whoAmI];
            if (player == null || !player.Active) return;

            if (_lastEffectTime.ContainsKey(player.Index))
            {
                if ((DateTime.UtcNow - _lastEffectTime[player.Index]).TotalMilliseconds < 200) return;
            }
            _lastEffectTime[player.Index] = DateTime.UtcNow;

            Item sword = player.TPlayer.HeldItem;
            // Perbaikan: Pakai .melee untuk kompatibilitas lebih luas
            if (sword.damage > 0 && sword.melee)
            {
                ApplyMeleeEffects(sword.type, args.Npc, player);
            }
        }

        private void ApplyMeleeEffects(int type, NPC target, TSPlayer player)
        {
            switch (type)
            {
                case ItemID.Muramasa:
                    CreateDustEffect(target.Center, 29, 3);
                    ApplyHomingEnergy(target, player, 226);
                    break;
                case ItemID.NightsEdge:
                    int heal = Main.rand.Next(1, 3);
                    player.TPlayer.statLife += heal;
                    player.TPlayer.HealEffect(heal);
                    CreateDustEffect(target.Center, 27, 4);
                    break;
                case ItemID.TerraBlade:
                    CreateDustEffect(target.Center, 107, 5);
                    ApplyHomingEnergy(target, player, 107);
                    break;
                default:
                    CreateDustEffect(target.Center, 31, 2);
                    break;
            }
        }

        private void ApplyHomingEnergy(NPC source, TSPlayer player, int dustType)
        {
            NPC closest = null;
            float maxDist = 350f;
            foreach (NPC n in Main.npc)
            {
                if (n != null && n.active && !n.friendly && n.whoAmI != source.whoAmI && !n.dontTakeDamage)
                {
                    float dist = Vector2.Distance(source.Center, n.Center);
                    if (dist < maxDist) { maxDist = dist; closest = n; }
                }
            }
            if (closest != null)
            {
                // Perbaikan: Cara ambil damage yang lebih universal
                int dmg = swordDamage(player.TPlayer) / 3;
                player.TPlayer.ApplyDamageToNPC(closest, dmg, 0f, 0, false);
                for (float i = 0; i < 1; i += 0.25f)
                {
                    int d = Dust.NewDust(Vector2.Lerp(source.Center, closest.Center, i), 1, 1, dustType);
                    Main.dust[d].noGravity = true;
                    Main.dust[d].velocity *= 0f;
                }
            }
        }

        // Helper untuk ambil damage senjata
        private int swordDamage(Player p)
        {
            return (int)(p.HeldItem.damage * p.meleeDamage);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if ((int)args.MsgID == 17)
            {
                using (var ms = new MemoryStream(args.Msg.readBuffer, args.Index, args.Length))
                {
                    using (var reader = new BinaryReader(ms))
                    {
                        byte action = reader.ReadByte();
                        int x = reader.ReadInt16();
                        int y = reader.ReadInt16();

                        if (action == 1)
                        {
                            TSPlayer player = TShock.Players[args.Msg.whoAmI];
                            if (player != null && player.Active && player.TPlayer.HeldItem.pick > 0)
                            {
                                ITile tile = Main.tile[x, y];
                                if (tile != null && tile.active() && TileID.Sets.Ore[tile.type])
                                {
                                    DestroyVein(x, y, tile.type, player);
                                }
                            }
                        }
                    }
                }
            }
        }

        private void DestroyVein(int x, int y, ushort tileType, TSPlayer player)
        {
            Queue<Point> nodes = new Queue<Point>();
            nodes.Enqueue(new Point(x, y));
            HashSet<Point> visited = new HashSet<Point>();
            int count = 0;

            while (nodes.Count > 0 && count < 100)
            {
                Point cur = nodes.Dequeue();
                if (visited.Contains(cur) || cur.X < 5 || cur.X > Main.maxTilesX - 5 || cur.Y < 5 || cur.Y > Main.maxTilesY - 5) continue;
                visited.Add(cur);

                ITile tile = Main.tile[cur.X, cur.Y];
                if (tile.active() && tile.type == tileType)
                {
                    count++;
                    WorldGen.KillTile(cur.X, cur.Y, false, false, false);
                    NetMessage.SendData(17, -1, -1, null, 1, cur.X, cur.Y);

                    nodes.Enqueue(new Point(cur.X + 1, cur.Y));
                    nodes.Enqueue(new Point(cur.X - 1, cur.Y));
                    nodes.Enqueue(new Point(cur.X, cur.Y + 1));
                    nodes.Enqueue(new Point(cur.X, cur.Y - 1));
                }
            }
        }

        private void CreateDustEffect(Vector2 pos, int type, int count)
        {
            for (int i = 0; i < count; i++)
            {
                // Perbaikan: NextFloat() tanpa parameter (0.0 sampai 1.0)
                float randX = (float)(Main.rand.NextDouble() * 2.0 - 1.0);
                float randY = (float)(Main.rand.NextDouble() * 2.0 - 1.0);
                int d = Dust.NewDust(pos, 4, 4, type, randX, randY);
                Main.dust[d].noGravity = true;
                Main.dust[d].scale = 0.8f;
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
