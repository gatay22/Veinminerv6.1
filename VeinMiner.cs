using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace AllArmorVisuals
{
    [ApiVersion(2, 1)]
    public class ArmorVisuals : TerrariaPlugin
    {
        public override string Name => "Dynamic Armor Visuals";
        public override string Author => "Gemini";
        public override Version Version => new Version(2, 3, 0);

        private float rotation = 0;

        public ArmorVisuals(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        private void OnUpdate(EventArgs args)
        {
            rotation += 0.12f; 

            foreach (TSPlayer player in TShock.Players)
            {
                if (player == null || !player.Active || player.TPlayer.dead) continue;

                Player p = player.TPlayer;
                
                // Wajib Full Set (Set Bonus Aktif)
                if (!string.IsNullOrEmpty(p.setBonus))
                {
                    int headId = p.armor[0].type;
                    int dustType = 15; // Default Putih (Untuk Early Ore)
                    int damage = 30;
                    int buffId = 0;
                    int count = 2;
                    bool isBee = false;

                    // --- LOGIKA FILTER VISUAL ---

                    // 1. BEE ARMOR
                    if (headId == 228) { dustType = 152; damage = 45; buffId = 20; count = 4; isBee = true; }
                    
                    // 2. PLATINUM (Tetap Biru karena lebih tinggi dari Gold)
                    else if (headId == 123) { dustType = 43; damage = 65; }

                    // 3. MOLTEN
                    else if (headId == 231) { dustType = 6; damage = 85; buffId = 24; count = 3; }

                    // 4. SHADOW / CRIMSON (Early Hardcore-ish)
                    else if (headId == 79 || headId == 80 || headId == 81) { dustType = 27; damage = 40; } // Shadow (Ungu)
                    else if (headId == 155) { dustType = 5; damage = 45; buffId = 31; } // Crimson (Merah + Ichor)

                    // 5. METEOR (Space Style)
                    else if (headId == 120 || headId == 121 || headId == 122) { dustType = 127; damage = 50; } // Meteor (Bara Api)

                    // 6. JUNGLE
                    else if (headId == 236 || headId == 237 || headId == 238) { dustType = 39; damage = 40; buffId = 20; } // Jungle (Hijau)

                    // 7. END GAME (Solar, Nebula, Vortex, Stardust)
                    else if (headId == 660) { dustType = 174; damage = 120; }
                    else if (headId == 661) { dustType = 229; damage = 120; }
                    else if (headId == 662) { dustType = 242; damage = 120; }
                    else if (headId == 663) { dustType = 135; damage = 120; }

                    // --- JIKA ORE BIASA (Iron, Lead, Silver, Tungsten, Tin, Copper, Gold) ---
                    // Logika: Jika tidak masuk kategori di atas, otomatis pakai dustType 15 (Putih)
                    
                    SpawnEffect(player, dustType, damage, buffId, count, isBee);
                }
            }
        }

        private void SpawnEffect(TSPlayer player, int dustType, int damage, int buffId, int count, bool isBee)
        {
            for (int i = 0; i < count; i++)
            {
                double angle = rotation + (i * (Math.PI * 2 / count));
                float radius = isBee ? 60 + Main.rand.Next(-5, 5) : 70;
                float posX = player.TPlayer.Center.X + (float)Math.Cos(angle) * radius;
                float posY = player.TPlayer.Center.Y + (float)Math.Sin(angle) * radius;

                Vector2 pos = new Vector2(posX, posY);
                int d = Dust.NewDust(pos, 8, 8, dustType, 0, 0, 100, default(Color), isBee ? 1.4f : 1.2f);
                Main.dust[d].noGravity = true;

                if (isBee) Main.dust[d].velocity = new Vector2(Main.rand.NextFloat(-0.5f, 0.5f), Main.rand.NextFloat(-0.5f, 0.5f));
                else Main.dust[d].velocity *= 0;

                foreach (NPC npc in Main.npc)
                {
                    if (npc.active && !npc.friendly && npc.life > 0)
                    {
                        if (Vector2.Distance(pos, npc.Center) < 35)
                        {
                            player.TPlayer.ApplyDamageToNPC(npc, damage, 4f, 0, false);
                            if (buffId > 0) npc.AddBuff(buffId, 180);
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
