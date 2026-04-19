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
        public override string Name => "VeinMiner & Adaptive Shields";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(1, 5, 2);

        public VeinMinerArmor(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnGameUpdate);
        }

        private void OnGameUpdate(EventArgs args)
        {
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                TSPlayer player = TShock.Players[i];
                if (player == null || !player.Active || player.Dead) continue;

                // --- VEIN MINER LOGIC ---
                if (player.TPlayer.controlUseItem && player.SelectedItem.pick > 0)
                {
                    int x = Player.tileTargetX;
                    int y = Player.tileTargetY;
                    if (x >= 0 && x < Main.maxTilesX && y >= 0 && y < Main.maxTilesY)
                    {
                        ITile tile = Main.tile[x, y];
                        if (tile.active() && (Main.tileOreFinderPriority[tile.type] > 0 || tile.type == 105))
                        {
                            DestroyVein(x, y, tile.type);
                        }
                    }
                }

                // --- ADAPTIVE SHIELD LOGIC ---
                int def = player.TPlayer.statDefense;
                if (def <= 0) continue;

                // Warna adaptif dari shirtColor (warna baju) player
                Color adaptiveColor = player.TPlayer.shirtColor;

                int diskCount = Math.Min(5, (def / 10) + 1);
                // Ganti Main.gameTime dengan DateTime agar build sukses
                float totalSeconds = (float)(DateTime.Now.TimeOfDay.TotalSeconds);
                float speed = totalSeconds * 5f;

                for (int j = 0; j < diskCount; j++)
                {
                    float angle = speed + (j * MathHelper.TwoPi / diskCount);
                    Vector2 offset = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * 65f;
                    Vector2 diskPos = player.TPlayer.Center + offset;

                    // Dust 267 (Custom Color Dust)
                    int d = Dust.NewDust(diskPos, 2, 2, 267, 0, 0, 100, adaptiveColor, 1.2f);
                    Main.dust[d].noGravity = true;

                    foreach (NPC npc in Main.npc)
                    {
                        if (npc != null && npc.active && !npc.friendly && Vector2.Distance(diskPos, npc.Center) < 40f)
                        {
                            int dmg = 25 + (int)(def * 1.5f);
                            npc.StrikeNPC(dmg, 12f, (npc.Center.X < player.X ? -1 : 1));
                            // Packet 28 adalah NpcStrike
                            NetMessage.SendData(28, -1, -1, null, npc.whoAmI, dmg, 12f);
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
                
                ITile t = Main.tile[p.X, p.Y];
                if (t.active() && t.type == type)
                {
                    WorldGen.KillTile(p.X, p.Y, false, false, false);
                    // Packet 17 adalah Tile Manipulation
                    NetMessage.SendData(17, -1, -1, null, 0, p.X, p.Y);
                    
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
