using System;
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

            // PERBAIKAN: Cara ambil player di TShock 6.1
            TSPlayer tsPlayer = TShock.Players[args.Msg.whoAmI];
            if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null) return;

            Player p = tsPlayer.TPlayer;
            Item item = p.inventory[p.selectedItem];

            // PERBAIKAN: Cek Melee secara manual (paling aman buat build)
            if (item != null && item.damage > 0 && item.melee && item.pick == 0 && item.axe == 0)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyMeleeGodEffects(tsPlayer, item);
                }
            }
        }

        private void ApplyMeleeGodEffects(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            
            // PERBAIKAN: Arah serangan berdasarkan arah hadap player (karena LastNetConfig error)
            float speed = 14f;
            Vector2 velocity = new Vector2(tsPlayer.TPlayer.direction * speed, 0f); 
            
            // Jika player mengayun ke atas/bawah sedikit
            if (tsPlayer.TPlayer.gravDir == -1f) velocity.Y = -speed;

            // 1. Proyektil Blue Flare (ID: 729)
            int p1 = Projectile.NewProjectile(null, pos, velocity, 729, item.damage * 2, 6f, tsPlayer.Index);
            
            // 2. Proyektil Solar Flare (ID: 608) untuk efek "Spark" di badan
            int p2 = Projectile.NewProjectile(null, pos, Vector2.Zero, 608, item.damage, 4f, tsPlayer.Index);

            // Kirim data ke semua player
            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p1);
            NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p2);
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
