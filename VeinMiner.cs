using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace UltimateVisualPlugin
{
    [ApiVersion(2, 1)]
    public class MasterPlugin : TerrariaPlugin
    {
        public override string Name => "Godly Armor & Accessory Effects";
        public override string Author => "Gemini";
        public override Version Version => new Version(1, 1, 2);

        private int timer = 0;

        public MasterPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        private void OnUpdate(EventArgs args)
        {
            timer++;
            if (timer % 15 != 0) return;

            foreach (TSPlayer tsPlayer in TShock.Players)
            {
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null || tsPlayer.Dead) continue;

                Player p = tsPlayer.TPlayer;

                // --- 1. LOGIKA ARMOR SHIELD ---
                int headID = p.armor[0].type;
                int armProj = -1;
                int armDmg = 10;

                if (headID == 79 || headID == 80 || headID == 81) { armProj = 157; armDmg = 12; }
                else if (headID == 231 || headID == 2763) { armProj = 15; armDmg = 40; }
                else if (headID >= 2851 && headID <= 2862) { armProj = 614; armDmg = 70; }

                if (armProj != -1 && timer % 60 == 0)
                {
                    int pID = Projectile.NewProjectile(null, p.Center, Vector2.Zero, armProj, armDmg, 5f, tsPlayer.Index);
                    if (pID >= 0 && pID < 1000)
                    {
                        Main.projectile[pID].timeLeft = 40;
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
                    }
                }

                // --- 2. LOGIKA AKSESORIS ---
                for (int i = 3; i <= 8; i++)
                {
                    Item acc = p.armor[i];
                    if (acc == null || acc.type == 0) continue;

                    // Efek Sayap
                    if (acc.wingSlot > 0 && p.velocity.Y != 0 && timer % 30 == 0)
                    {
                        int wID = Projectile.NewProjectile(null, p.Bottom, Vector2.Zero, 502, 5, 0f, tsPlayer.Index);
                        if (wID >= 0 && wID < 1000)
                        {
                            Main.projectile[wID].timeLeft = 20;
                            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, wID);
                        }
                    }

                    // Efek Sepatu
                    if ((acc.type == 54 || acc.type == 1862 || acc.type == 5000) && Math.Abs(p.velocity.X) > 8)
                    {
                        int sID = Projectile.NewProjectile(null, p.Bottom, Vector2.Zero, 612, 8, 0f, tsPlayer.Index);
                        if (sID >= 0 && sID < 1000)
                        {
                            Main.projectile[sID].timeLeft = 15;
                            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, sID);
                        }
                    }
                } // Tutup Loop Aksesoris
            } // Tutup Foreach Player
        } // Tutup OnUpdate

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            base.Dispose(disposing);
        }
    }
}
