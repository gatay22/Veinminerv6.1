using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;

namespace AccessoryOverdrive
{
    [ApiVersion(2, 1)]
    public class ReforgePlugin : TerrariaPlugin
    {
        public override string Name => "Accessory 10 Percent Boost Real Final";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 3);

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

                // Slot 3 sampai 9 adalah slot aksesoris
                for (int j = 3; j < 10; j++)
                {
                    Item item = p.armor[j];
                    if (item == null || item.type == 0 || item.prefix == 0) continue;

                    // MENGGUNAKAN VARIABEL DASAR VANILLA (PASTI BISA BUILD)
                    switch (item.prefix)
                    {
                        case 65: // Menacing (+4% Damage)
                            float boostDmg = 0.06f; // Tambah 6%
                            p.meleeDamage += boostDmg;
                            p.magicDamage += boostDmg;
                            p.rangedDamage += boostDmg;
                            p.minionDamage += boostDmg;
                            break;

                        case 68: // Lucky (+4% Crit)
                            int boostCrit = 6; // Tambah 6%
                            p.meleeCrit += boostCrit;
                            p.magicCrit += boostCrit;
                            p.rangedCrit += boostCrit;
                            break;

                        case 62: // Warding (+4 Defense)
                            p.statDefense += 6;
                            break;

                        case 67: // Quick (+4% Move Speed)
                            p.moveSpeed += 0.06f;
                            break;

                        case 66: // Violent (+4% Melee Speed)
                            p.meleeSpeed += 0.06f;
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
