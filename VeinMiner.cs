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
        public override Version Version => new Version(1, 1, 1);

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

                // --- BAGIAN 1: ARMOR SHIELD (DAMAGE TINGGI) ---
                int headID = p.armor[0].type;
                int armProj = -1;
                int armDmg = 10;

                // Logika Armor (Sama seperti sebelumnya)
                if (headID == 79 || headID == 80 || headID == 81) { armProj = 157; armDmg = 12; }
                else if (headID == 231 || headID == 2763) { armProj = 15; armDmg = 40; }
                else if (headID >= 2851 && headID <= 2862) { armProj = 614; armDmg = 70; }

                if (armProj != -1 && timer % 60 == 0)
                {
                    int pID = Projectile.NewProjectile(null, p.Center, Vector2.Zero, armProj, armDmg, 5f, tsPlayer.Index);
                    if (pID < 1000)
                    {
                        Main.projectile[pID].timeLeft = 40;
                        NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
                    }
                }

                //
