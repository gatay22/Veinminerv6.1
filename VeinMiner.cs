using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System;
using System.Collections.Generic;

namespace VeinMinerArmorPlugin
{
    [ApiVersion(2, 1)]
    public class VeinMinerArmor : TerrariaPlugin
    {
        public override string Name => "VeinMiner & Color Adaptive Shields";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(1.5, 1);

        public VeinMinerArmor(Main game) : base(game) { }

        public override void Initialize()
        {
            // Perbaikan nama Hook ke PostPlayerUpdate
            ServerApi.Hooks.NetGreetPlayer.Register(this, OnGreetPlayer);
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnGreetPlayer(GreetPlayerEventArgs args)
        {
            // Digunakan hanya untuk inisialisasi jika diperlukan, 
            // Vein Miner kita pindahkan ke GameUpdate agar lebih stabil
        }

        // --- SECTION 1 & 2 GABUNGAN (Optimasi Server) ---
        private void OnGameUpdate(EventArgs args)
        {
            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null || !player.Active || player.Dead) continue;

                // --- VEIN MINER LOGIC ---
                if (player.TPlayer.controlUseItem && player.SelectedItem.pick > 0)
                {
                    int x = Player.tileTargetX;
                    int y = Player.tileTargetY;
                    if (x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY)
                    {
                        Tile tile = Main.tile[x, y];
                        if (tile.active() && (Main.tileOreFinderPriority[tile.type] > 0 || tile.type == 105))
                        {
                            DestroyVein(x, y, tile.type);
                        }
                    }
                }

                // --- ADAPTIVE SHIELD LOGIC ---
                int def = player.TPlayer.statDefense;
                if (def <= 0) continue;

                // Ambil warna adaptif dari baju player
                Color adaptiveColor = player.TPlayer.shirtColor;
                if (player.TPlayer.body > 0)
                {
                    // Gunakan warna dasar armor
                    adaptiveColor = player.TPlayer.GetImmuneAlphaColor(Color.White);
                }

                int diskCount = Math.Min(5, (def / 10) + 1);
                float speed = (float)Main.gameTime.TotalGameTime.TotalSeconds * 5f;

                for (int i = 0; i < diskCount; i++)
                {
                    float angle = speed + (i * MathHelper.TwoPi / diskCount);
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 65f;
                    Vector2 diskPos = player.TPlayer.Center + offset;

                    int d = Dust.NewDust(diskPos, 2, 2, 267, 0, 0, 100, adaptiveColor, 1.2f);
                    Main.dust[d].noGravity = true;

                    // Damage berkelanjutan
                    foreach (NPC npc in Main.npc)
                    {
                        if (npc != null && npc.active && !npc.friendly && Vector2.Distance(diskPos, npc.Center) < 40f)
                        {
                            int dmg = 25 + (int)(def * 1.5f);
                            npc.StrikeNPC(dmg, 12f, (npc.Center.X < player.X ? -1 : 1));
                            NetMessage.SendData((int)PacketTypes.NpcStrike, -1, -1, null, npc.whoAmI, dmg, 12f);
                        }
                    }
                }
            }
        }

        private void DestroyVein(int x, int y, ushort type)
        {
            Queue<Point> nodes = new Queue<Point>();
            nodes.Enqueue(new Point(x, y));
            int count = 0;
            while (nodes.Count > 0 && count < 180)
            {
                Point p = nodes.Dequeue();
                if (p.X < 0 || p.X >= Main.maxTilesX || p.Y < 0 || p.Y >= Main.maxTilesY) continue;
                Tile t = Main.tile[p.X, p.Y];
                if (t.active() && t.type == type)
                {
                    WorldGen.KillTile(p.X, p.Y, false, false, false);
                    NetMessage.SendData((int)PacketTypes.TileManipulation, -1, -1, null, 0, p.X, p.Y);
                    nodes.Enqueue(new Point(p.X + 1, p.Y));
                    nodes.Enqueue(new Point(p.X - 1, p.Y));
                    nodes.Enqueue(new Point(p.X, p.Y + 1));
                    nodes.Enqueue(new Point(p.X, p.Y - 1));
                    count++;
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.GameUpdate.Deregister(this, OnGameUpdate);
            }
            base.Dispose(disposing);
        }
    }
}
