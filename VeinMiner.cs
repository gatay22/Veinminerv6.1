using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace AccessoryOverdrive
{
    [ApiVersion(2, 1)]
    public class ReforgePlugin : TerrariaPlugin
    {
        public override string Name => "Accessory 10 Percent Boost Final";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 2);

        public ReforgePlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
        }

        private void OnUpdate(EventArgs args)
        {
            for (int i = 0; i < TShock.Players.Length; i++)
            {
                TSPlayer tsPlayer = TShock.Players[i];
                if (tsPlayer == null || !tsPlayer.Active || tsPlayer.TPlayer == null) continue;

                Player p = tsPlayer.TPlayer;

                // Loop slot aksesoris
                for (int j = 3; j < 10; j++)
                {
                    Item item = p.armor[j];
                    if (item == null || item.type == 0 || item.prefix == 0) continue;

                    // PAKAI LOGIKA DASAR (Fixing CS1061)
                    switch (item.prefix)
                    {
                        case 65: // Menacing
                            // Di 1.4.4+, kita akses lewat damage multiplier
                            p.GetDamage(DamageClass.Generic) += 0.06f;
                            break;
                        case 68: // Lucky
                            p.GetCritChance(DamageClass.Generic) += 6f;
                            break;
                        case 62: // Warding
                            p.statDefense += 6;
                            break;
                        case 67: // Quick
                            p.moveSpeed += 0.06f;
                            break;
                        case 66: // Violent
                            p.GetAttackSpeed(DamageClass.Melee) += 0.06f;
                            break;
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            base.Dispose(disposing);
        }
    }
}
