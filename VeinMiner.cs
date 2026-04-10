using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace PersonalizedRangedArsenal
{
    [ApiVersion(2, 1)]
    public class RangedPlugin : TerrariaPlugin
    {
        public override string Name => "Personalized Ranged Elements";
        public override string Author => "Gemini";
        public override Version Version => new Version(2, 6, 0);

        public RangedPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerUpdate) return;

            TSPlayer tsPlayer = TShock.Players[args.Msg.whoAmI];
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null) return;

            Player p = tsPlayer.TPlayer;
            Item item = p.inventory[p.selectedItem];

            // Cek jika item adalah senjata Ranged dengan damage
            if (item != null && item.damage > 0 && item.ranged)
            {
                // Trigger saat senjata ditembakkan (frame awal animasi)
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyUniqueRangedEffect(tsPlayer, item);
                }
            }
        }

        private void ApplyUniqueRangedEffect(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            int dir = tsPlayer.TPlayer.direction;
            Vector2 vel = new Vector2(dir * 15f, 0f); // Kecepatan peluru tambahan
            int id = item.type;
            Random rand = new Random();

            int projID = -1;
            int count = 1;
            float dmgMult = 1f;

            // 1. ELEMEN API (Molten Fury, Hellwing Bow, Phoenix Blaster)
            if (id == 120 || id == 3247 || id == 197)
            {
                projID = 15; // Fireball
                count = 2;
            }
            // 2. ELEMEN ES (Ice Bow, Snowman Cannon)
            else if (id == 725 || id == 1946)
            {
                projID = 118; // Frost Beam
            }
            // 3. ELEMEN LISTRIK/HI-TECH (Megashark, Laser Rifle, Gatligator)
            else if (id == 533 || id == 434 || id == 1255)
            {
                projID = 440; // Laser biru elektrik
                count = 2;
            }
            // 4. ELEMEN DARK/CORRUPTION (Onyx Blaster, Dart Rifle)
            else if (id == 3788 || id == 3019)
            {
                projID = 496; // Shadowflame
            }
            // 5. ELEMEN NATURE/POISON (Chlorophyte Shotbow, Bee's Knees)
            else if (id == 1229 || id == 2888)
            {
                projID = 226; // Chlorophyte Seeker (Ngejar musuh)
            }
            // 6. ELEMEN COSMIC/ENDGAME (S.D.M.G, Phantasm, Vortex Beater)
            else if (id == 1553 || id == 3475 || id == 3476)
            {
                projID = 614; // Nebula Sphere
                count = 3;
                dmgMult = 1.2f;
            }
            // 7. ELEMEN DARAH/CRIMSON (Dart Pistol, Tendon Bow)
            else if (id == 3020 || id == 796)
            {
                projID = 305; // Ichor Splash
            }

            // Eksekusi penembakan proyektil elemen
            if (projID != -1)
            {
                for (int i = 0; i < count; i++)
                {
                    float spread = MathHelper.ToRadians(rand.Next(-8, 9));
                    int pID = Projectile.NewProjectile(null, pos, vel.RotatedBy(spread), projID, (int)(item.damage * dmgMult), 3f, tsPlayer.Index);
                    NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
                }
            }
            // DEFAULT: Efek laser Twins standar untuk senjata lainnya
            else
            {
                int pID = Projectile.NewProjectile(null, pos, vel, 83, (int)(item.damage * 0.7), 2f, tsPlayer.Index);
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
