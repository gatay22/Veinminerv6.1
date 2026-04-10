using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace RangedTwinsStandard
{
    [ApiVersion(2, 1)]
    public class RangedPlugin : TerrariaPlugin
    {
        public override string Name => "Twins Laser Ranged Dynamic";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 1);

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

            // Cek jika senjata Ranged
            if (item != null && item.damage > 0 && item.ranged)
            {
                // Trigger saat senjata ditembakkan
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyTwinsLaser(tsPlayer, item);
                }
            }
        }

        private void ApplyTwinsLaser(TSPlayer tsPlayer, Item item)
        {
            // Peluang 20% (1 dari 5 tembakan)
            if (Main.rand.Next(5) != 0) return;

            Vector2 pos = tsPlayer.TPlayer.Center;
            Vector2 targetPos = new Vector2(tsPlayer.TPlayer.lastMouseX + Main.screenPosition.X, tsPlayer.TPlayer.lastMouseY + Main.screenPosition.Y);
            Vector2 baseVelocity = Vector2.Normalize(targetPos - pos) * 16f;

            int laserProjID = 83; // Laser Merah Twins (Retinazer)

            // Tembakkan 5 laser sekaligus
            for (int i = 0; i < 5; i++)
            {
                // Spread tipis agar laser menyebar seperti kipas
                float spread = MathHelper.ToRadians(-8f + (i * 4f)); 
                Vector2 shotVelocity = baseVelocity.RotatedBy(spread);

                // DAMAGE MENGIKUTI SENJATA (item.damage)
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
