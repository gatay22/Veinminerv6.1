using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace WhipRework
{
    [ApiVersion(2, 1)]
    public class DynamicWhipPlugin : TerrariaPlugin
    {
        public override string Name => "Homing Whip Rework Fixed";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 1);

        public DynamicWhipPlugin(Main game) : base(game) { }

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

            // Cek apakah item adalah Summoner Whip (Summon damage dan bukan pickaxe)
            if (item != null && item.damage > 0 && item.summon && item.pick == 0)
            {
                if (p.itemAnimation == p.itemAnimationMax - 1 && p.itemAnimation > 0)
                {
                    ApplyHomingWhip(tsPlayer, item);
                }
            }
        }

        private void ApplyHomingWhip(TSPlayer tsPlayer, Item item)
        {
            Vector2 pos = tsPlayer.TPlayer.Center;
            int projID = 307; // Default: Chlorophyte Orb (Homing Kuat)
            
            // Menggunakan Name dengan huruf kapital atau cek
