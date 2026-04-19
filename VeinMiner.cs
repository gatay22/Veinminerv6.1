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
        public override Version Version => new Version(1.0, 2);

        public LootNotifier(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            // Packet 90 is ItemOwner - synced when someone picks up an item
            if (args.MsgId == PacketTypes.ItemOwner)
            {
                // In TShock v6.1, Main.item[index] might be a WorldItem. 
                // We use the ID to get a temporary Item instance for checking stats.
                int itemIndex = args.number;
                Item item = Main.item[itemIndex];

                if (item != null && !string.IsNullOrEmpty(item.Name))
                {
                    // Filter for Rare items (Rarity 4+ or rare fishing gear)
                    if (item.rare >= 4 || (item.fishingPole > 0 && item.rare >= 3) || item.questItem)
                    {
                        // args.ignoreClient usually holds the Player Index for this packet type
                        int playerIndex = args.ignoreClient;
                        
                        // Validate player index range
                        if (playerIndex >= 0 && playerIndex < TShock.Players.Length)
                        {
                            TSPlayer player = TShock.Players[playerIndex];

                            if (player != null && player.Active)
                            {
                                // English Broadcast Message with Item Tag [i:ID]
                                string broadCastMsg = $"[c/00FFFF:RARE FIND!] [c/E1E1E1:{player.Name}] just found [i:{item.type}] [c/FFD700:{item.Name}]!";
                                
                                TShock.Utils.Broadcast(broadCastMsg, Color.Cyan);

                                // Server Console Logging
                                Console.ForegroundColor = ConsoleColor.Cyan;
                                Console.WriteLine($"[Loot] {player.Name} obtained {item.Name} (ID: {item.type})");
                                Console.ResetColor();
                            }
                        }
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
