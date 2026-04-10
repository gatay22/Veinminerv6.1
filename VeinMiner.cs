using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace AdaptiveArmorOnly
{
    [ApiVersion(2, 1)]
    public class ArmorPlugin : TerrariaPlugin
    {
        public override string Name => "Adaptive Armor Shields";
        public override string Author => "Gemini";
        public override Version Version => new Version(1, 0, 1);

        private int timer = 0;

        public ArmorPlugin(Main game) : base(game) { }

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
                int headID = p.armor[0].type;

                int projID = -1;
                int damage = 10;
                float kb = 3f;

                // 1. ORE AWAL
                if (headID == 79 || headID == 80 || headID == 81 || headID == 76 || headID == 77 || headID == 78) 
                {
                    projID = 157;
                    damage = 12;
                }
                // 2. ORE MEWAH
                else if (headID == 82 || headID == 83 || headID == 414 || headID == 415)
                {
                    projID = 156;
                    damage = 18;
                }
                // 3. ARMOR API
                else if (headID == 231 || headID == 2763)
                {
                    projID = 15;
                    damage = 35;
                    kb = 6f;
                }
                // 4. ARMOR GELAP
                else if (headID == 101 || headID == 102)
                {
                    projID = 496;
                    damage = 25;
                }
                // 5. ARMOR DARAH
                else if (headID == 792)
                {
                    projID = 305;
                    damage = 25;
                }
                // 6. ARMOR HUTAN
                else if (headID == 228 || headID == 1001 || headID == 1002 || headID == 1003)
                {
                    projID = 228;
                    damage = 20;
                }
                // 7. ARMOR LEBAH
                else if (headID == 2361)
                {
                    projID = 181;
                    damage = 15;
                }
                // 8. ARMOR ES
                else if (headID == 684)
                {
                    projID = 118;
                    damage = 28;
                }
                // 9. ARMOR SUCI
                else if (headID == 553 || headID == 558 || headID == 559)
                {
                    projID = 173;
                    damage = 30;
                }
                // 10. ARMOR DEWA
                else if (headID >= 2851 && headID <= 2862)
                {
                    projID = 614;
                    damage = 60;
                    kb = 10f;
                }

                if (projID != -1 && timer % 60 == 0)
                {
                    for (int i = 0; i < 2; i++)
                    {
                        Vector2 offset = new Vector2(i == 0 ? 38 : -38, -5);
                        int pID = Projectile.NewProjectile(null, p.Center + offset, Vector2.Zero, projID, damage, kb, tsPlayer.Index);
                        if (pID != 1000) // Cek limit proyektil Terraria
                        {
                            Main.projectile[pID].timeLeft = 40;
                            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
                        }
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            base.Dispose(disposing);
        }
    }
}
