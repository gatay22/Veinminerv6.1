using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace UniqueMeleeMaster
{
    [ApiVersion(2, 1)]
    public class MeleePlugin : TerrariaPlugin
    {
        public override string Name => "Personalized Melee Arsenal";
        public override string Author => "Gemini";
        public override Version Version => new Version(2, 4, 0);

        public MeleePlugin(Main game) : base(game) { }

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

            if (item != null && item.damage > 0 && item.melee)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyUniqueMeleeEffect(tsPlayer, item);
                }
            }
        }

        private void ApplyUniqueMeleeEffect(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            int dir = tsPlayer.TPlayer.direction;
            Vector2 vel = new Vector2(dir * 14f, 0f);
            int id = item.type;

            int projID = -1;
            float damageMult = 1f;

            // 1. KATEGORI API (Volcano, Molten, Solar)
            if (id == 121 || id == 3473 || id == 112) projID = 15; 
            
            // 2. KATEGORI ES (Ice Blade, Frostbrand)
            else if (id == 724 || id == 674 || id == 675) projID = 118;

            // 3. KATEGORI SHADOW/DARK (Night's Edge, Breaker Blade)
            else if (id == 273 || id == 46) projID = 496; // Shadowflame burst

            // 4. KATEGORI BLOOD/CRIMSON (Blood Butcherer, Blademaster)
            else if (id == 795 || id == 3211) projID = 305; // Ichor splash

            // 5. KATEGORI HALLOWED/LIGHT (Excalibur, Gungnir)
            else if (id == 368 || id == 550) projID = 156; // Beam Sword ray

            // 6. KATEGORI NATURE (Blade of Grass, Muramasa)
            else if (id == 190 || id == 155) projID = 228; // Spore cloud

            // 7. KATEGORI COSMIC/ENDGAME (Terra Blade, Influx Waver, Star Wrath)
            else if (id == 757 || id == 2880 || id == 3063) 
            {
                projID = 614; // Nebula sphere
                damageMult = 1.2f;
            }
            
            // 8. KATEGORI KHUSUS ZENITH (Kalau ada yang punya)
            else if (id == 4956) projID = 706;

            // Jika masuk kategori, tembak!
            if (projID != -1)
            {
                int pID = Projectile.NewProjectile(null, pos, vel, projID, (int)(item.damage * damageMult), 5f, tsPlayer.Index);
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
            }
            // DEFAULT: Efek acak tipis-tipis buat pedang kayu/besi biasa
            else
            {
                int[] common = { 132, 157 }; // Terra beam kecil atau ice sickle
                int pID = Projectile.NewProjectile(null, pos, vel, common[new Random().Next(2)], (int)(item.damage * 0.5), 2f, tsPlayer.Index);
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
