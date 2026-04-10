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
        public override Version Version => new Version(1, 0, 0);

        private int timer = 0;

        public ArmorPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        private void OnUpdate(EventArgs args)
        {
            timer++;
            // Update setiap 15 tick agar performa server tetap enteng
            if (timer % 15 != 0) return;

            foreach (TSPlayer tsPlayer in TShock.Players)
            {
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null || tsPlayer.Dead) continue;

                Player p = tsPlayer.TPlayer;
                int headID = p.armor[0].type; // Mendeteksi helm/topi armor

                int projID = -1;
                int damage = 10;
                float kb = 3f;

                // --- DAFTAR KOMPLIT EFEK ARMOR ---

                // 1. ORE AWAL (Copper, Iron, Lead, Tin, Wood)
                if (headID == 79 || headID == 80 || headID == 81 || headID == 76 || headID == 77 || headID == 78) 
                {
                    projID = 157; // Metal Shard (Piringan Besi)
                    damage = 12;
                }
                // 2. ORE MEWAH (Gold, Platinum, Silver, Tungsten)
                else if (headID == 82 || headID == 83 || headID == 414 || headID == 415)
                {
                    projID = 156; // Gold Beam (Kilatan Emas)
                    damage = 18;
                }
                // 3. ARMOR API (Molten, Solar Flare)
                else if (headID == 231 || headID == 2763)
                {
                    projID = 15; // Fireball (Bola Api)
                    damage = 35;
                    kb = 6f;
                }
                // 4. ARMOR GELAP (Shadow, Ancient Shadow)
                else if (headID == 101 || headID == 102)
                {
                    projID = 496; // Shadowflame (Api Ungu)
                    damage = 25;
                }
                // 5. ARMOR DARAH (Crimson)
                else if (headID == 792)
                {
                    projID = 305; // Ichor Splash (Cipratan Darah)
                    damage = 25;
                }
                // 6. ARMOR HUTAN (Jungle, Chlorophyte)
                else if (headID == 228 || headID == 1001 || headID == 1002 || headID == 1003)
                {
                    projID = 228; // Spore Cloud (Awan Racun)
                    damage = 20;
                }
                // 7. ARMOR LEBAH (Bee Armor)
                else if (headID == 2361)
                {
                    projID = 181; // Bees (Lebah Penjaga)
                    damage = 15;
                }
                // 8. ARMOR ES (Frost Armor)
                else if (headID == 684)
                {
                    projID = 118; // Frost Beam (Laser Es)
                    damage = 28;
                }
                // 9. ARMOR SUCI (Hallowed Armor)
