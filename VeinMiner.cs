using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace AccessoryOverdrive
{
    [ApiVersion(2, 1)]
    public class ReforgePlugin : TerrariaPlugin
    {
        public override string Name => "Accessory 10 Percent Boost";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 0);

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

                // Slot 3 sampai 9 adalah slot KHUSUS AKSESORIS di Terraria
                // (Slot 0-2 itu Armor, jadi kita lewati)
                for (int j = 3; j < 10; j++)
                {
                    Item item = p.armor[j];
                    if (item == null || item.type == 0 || item.prefix == 0) continue;

                    // Logika Tambahan 6% (Biar total jadi 10%)
                    switch (item.prefix)
                    {
                        case 65: // Menacing (+4% Damage)
                            p.GetDamage(DamageClass.Generic) += 0.06f;
                            break;
                        case 68: // Lucky (+4% Crit)
                            p.GetCritChance(DamageClass.Generic) += 6f;
                            break;
                        case 62: // Warding (+4 Defense)
                            p.statDefense += 6; // Bonus flat defense
                            break;
                        case 67: // Quick (+4% Move Speed)
                            p.moveSpeed += 0.06f;
                            break;
                        case 66: // Violent (+4% Melee Speed)
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
