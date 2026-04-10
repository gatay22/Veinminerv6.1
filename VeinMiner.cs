using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace RangedVarietyPlugin
{
    [ApiVersion(2, 1)]
    public class RangedPlugin : TerrariaPlugin
    {
        public override string Name => "Ultimate Ranged Variety";
        public override string Author => "Player";
        public override Version Version => new Version(2, 1, 0);

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

            // Filter hanya senjata Ranged
            if (item != null && item.damage > 0 && item.ranged)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    SpawnRandomRangedAttack(tsPlayer, item);
                }
            }
        }

        private void SpawnRandomRangedAttack(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            int dir = tsPlayer.TPlayer.direction;
            Vector2 vel = new Vector2(dir * 14f, 0f); // Kecepatan dasar
            Random rand = new Random();

            // Kumpulan ID Proyektil Keren
            // 83: Laser Merah, 84: Laser Hijau, 100: Death Laser, 226: Chlorophyte, 631: Daybreak
            int[] projPool = { 83, 84, 100, 226, 631, 242, 440, 118 };
            
            // Pilih 1 serangan secara acak dari pool
            int selectedProj = projPool[rand.Next(projPool.Length)];

            // Variasi jumlah proyektil (antara 1 sampai 3)
            int amount = rand.Next(1, 4);

            for (int i = 0; i < amount; i++)
            {
                // Kasih sedikit spread (acak arah) supaya tidak numpuk di satu garis
                float spread = MathHelper.ToRadians(rand.Next(-15, 16));
                Vector2 finalVel = vel.RotatedBy(spread);

                int pID = Projectile.NewProjectile(null, pos, finalVel, selectedProj, item.damage, 3f, tsPlayer.Index);
                
                // Beritahu server untuk kirim proyektil ke semua player
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, pID);
            }

            // Bonus: 10% peluang muncul ledakan Shadowflame di posisi player saat nembak
            if (rand.Next(10) == 0)
            {
                int boom = Projectile.NewProjectile(null, pos, Vector2.Zero, 496, item.damage, 5f, tsPlayer.Index);
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, boom);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
