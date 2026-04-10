using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace RangedTwinsFinal
{
    [ApiVersion(2, 1)]
    public class RangedPlugin : TerrariaPlugin
    {
        public override string Name => "Twins Laser Ranged Fixed";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 2);

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

            if (item != null && item.damage > 0 && item.ranged)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyTwinsLaser(tsPlayer, item);
                }
            }
        }

        private void ApplyTwinsLaser(TSPlayer tsPlayer, Item item)
        {
            if (Main.rand.Next(5) != 0) return;

            Vector2 pos = tsPlayer.TPlayer.Center;
            
            // Karena lastMouseX tidak ada di server, kita gunakan arah hadap player (direction)
            // 16f adalah kecepatan laser
            Vector2 baseVelocity = new Vector2(tsPlayer.TPlayer.direction * 16f, 0f);

            int laserProjID = 83; // Laser Merah Twins

            for (int i = 0; i < 5; i++)
            {
                // Variasi sudut supaya menyebar (kipas)
                float spread = MathHelper.ToRadians(-10f + (i * 5f)); 
                Vector2 shotVelocity = baseVelocity.RotatedBy(spread);

                // Damage murni mengikuti senjata
                int proj = Projectile.NewProjectile(null, pos, shotVelocity, laserProjID, item.damage, 4f, tsPlayer.Index);
                
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, proj);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
