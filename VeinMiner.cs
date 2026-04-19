using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System;

namespace RareLootNotifier
{
    [ApiVersion(2, 1)]
    public class LootNotifier : TerrariaPlugin
    {
        public override string Name => "Rare Loot & Fishing Notifier";
        public override string Author => "Gemini AI";
        public override string Description => "Broadcasts a message when a player finds rare loot or fishing items.";
        public override Version Version => new Version(1.0, 1);

        public LootNotifier(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            // Packet 90 (ItemOwner) syncs item ownership to all clients
            if (args.MsgId == PacketTypes.ItemOwner)
            {
                Item item = Main.item[args.number];

                // --- RARITY FILTER ---
                // rare >= 4: Light Red, Pink, Light Purple, Lime, Yellow, Cyan, Red, Purple
                // rare == -1: Quest items or special drops
                if (item.rare >= 4 || (item.fishingPole > 0 && item.rare >= 3) || item.questItem)
                {
                    TSPlayer player = TShock.Players[item.owner];

                    if (player != null && player.Active && !string.IsNullOrEmpty(item.Name))
                    {
                        // English Broadcast Message
                        // [i:ID] displays the item icon, [c/XXXXXX:text] colors the text
                        string broadCastMsg = $"[c/00FFFF:RARE FIND!] [c/E1E1E1:{player.Name}] just found [i:{item.type}] [c/FFD700:{item.Name}]!";
                        
                        TShock.Utils.Broadcast(broadCastMsg, Color.Cyan);

                        // Console Log (Server Side)
                        Console.ForegroundColor = ConsoleColor.Cyan;
                        Console.WriteLine($"[Loot] {player.Name} obtained {item.Name} (ID: {item.type})");
                        Console.ResetColor();
                    }
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            }
            base.Dispose(disposing);
        }
    }
}
