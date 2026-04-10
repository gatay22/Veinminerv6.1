using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace MeleeProjectileRework
{
    [ApiVersion(2, 1)]
    public class MeleePlugin : TerrariaPlugin
    {
        public override string Name => "Unique Melee Projectiles";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 0);

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

            // Cek apakah itu senjata Melee (dan bukan tools seperti kapak/pickaxe)
            if (item != null && item.damage > 0 && item.melee && item.pick == 0 && item.axe == 0)
            {
                // Trigger hanya saat animasi ayunan pedang dimulai
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ShootUniqueProjectile(tsPlayer, item);
                }
            }
        }

        private void ShootUniqueProjectile(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            Vector2 velocity = new Vector2(tsPlayer.TPlayer.direction * 12f, 0f);
            int projID;

            // LOGIKA PEMBEDA PROYEKTIL BERDASARKAN NAMA PEDANG
            string name = item.Name ?? "";

            if (name.Contains("Star")) 
                projID = 12; // Falling Star
            else if (name.Contains("Fire") || name.Contains("Fiery") || name.Contains("Flame")) 
                projID = 188; // Magma Ball
            else if (name.Contains("Ice") || name.Contains("Frost") || name.Contains("Frozen")) 
                projID = 118; // Frost Beam
            else if (name.Contains("Grass") || name.Contains("Blade of Grass") || name.Contains("Thorn")) 
                projID = 45; // Spore Cloud
            else if (name.Contains("Excalibur") || name.Contains("Light")) 
                projID = 156; // Hallowed Beam (Laser Kuning)
            else if (name.Contains("Night") || name.Contains("Dark")) 
                projID = 274; // Shadow Beam
            else 
                projID = 157; // Laser standar (Purple Beam) untuk pedang lainnya

            // Tembakkan proyektil dengan damage sesuai senjata
            int proj = Projectile.NewProjectile(null, pos, velocity, projID, item.damage, 5f, tsPlayer.Index);
            
            // Kirim data ke semua player agar visualnya muncul
            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, proj);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
