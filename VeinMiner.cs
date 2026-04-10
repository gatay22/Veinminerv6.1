using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace MeleeRework
{
    [ApiVersion(2, 1)]
    public class MeleeReworkPlugin : TerrariaPlugin
    {
        public override string Name => "Ultimate Melee Rework";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 0);

        public MeleeReworkPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.PlayerUpdate) return;

            TSPlayer tsPlayer = TShock.Players[args.Player.whoAmI];
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.SelectedItem == null) return;

            Player p = tsPlayer.TPlayer;
            Item item = tsPlayer.SelectedItem;

            // DETEKSI SEMUA MELEE: Pedang, Tombak, Yoyo, Flail, dll.
            // Kita kecualikan alat kerja (Pickaxe/Axe) biar gak ganggu pas nambang.
            if (item.damage > 0 && item.countsAsClass(DamageClass.Melee) && item.pick == 0 && item.axe == 0)
            {
                // Cek saat animasi ayunan dimulai
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyMeleeGodEffects(tsPlayer, item);
                }
            }
        }

        private void ApplyMeleeGodEffects(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            
            // Hitung arah mouse player
            float speed = 14f;
            float angle = (float)Math.Atan2(tsPlayer.LastNetConfig.Y - pos.Y, tsPlayer.LastNetConfig.X - pos.X);
            Vector2 velocity = new Vector2((float)Math.Cos(angle), (float)Math.Sin(angle)) * speed;

            // 1. PROYEKTIL UTAMA: Blue Flare (ID: 729 - Efeknya keren banget)
            int p1 = Projectile.NewProjectile(null, pos, velocity, 729, item.damage * 2, 6f, tsPlayer.Index);
            
            // 2. PROYEKTIL TAMBAHAN: Solar Flare (ID: 608 - Efek api berputar)
            // Biar mepet sama player, buat efek tebasan area
            int p2 = Projectile.NewProjectile(null, pos, velocity * 0.5f, 608, item.damage, 4f, tsPlayer.Index);

            // Kirim data ke semua player biar visualnya muncul
            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p1);
            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p2);
            
            // Efek suara biar kerasa berat
            tsPlayer.SendData(PacketTypes.PlaySound, "", 2, pos.X, pos.Y, 71); 
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
