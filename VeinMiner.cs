using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System;

namespace RareLootNotifier
{
    // Fix: Explicitly use 2, 1 as integers
    [ApiVersion(2, 1)]
    public class LootNotifier : TerrariaPlugin
    {
        public override string Name => "Rare Loot & Fishing Notifier";
        public override string Author => "Gemini AI";
        public override string Description => "Broadcasts a message when a player finds rare loot.";
        public override Version Version => new Version(1, 0, 3);

        public LootNotifier(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            if (args.MsgId == PacketTypes.ItemOwner)
            {
                int itemIndex = args.number;
                
                // Fix: Cast WorldItem to Item explicitly or access properties safely
                if (itemIndex >= 0 && itemIndex < Main.item.Length)
                {
                    // In v6.1, we cast to (Item) to access standard properties
                    Item item = (Item)Main.item[itemIndex];

                    if (item != null && !string.IsNullOrEmpty(item.Name))
                    {
                        if (item.rare >= 4 || (item.fishingPole > 0 && item.rare >= 3) || item.questItem)
                        {
                            // In this packet, ignoreClient usually represents the player gaining the item
                            int playerIndex = args.ignoreClient;
                            
                            if (playerIndex >= 0 && playerIndex < TShock.Players.Length)
                            {
                                TSPlayer player = TShock.Players[playerIndex];

                                if (player != null && player.Active)
                                {
                                    string broadCastMsg = $"[c/00FFFF:RARE FIND!] [c/E1E1E1:{player.Name}] just found [i:{item.type}] [c/FFD700:{item.Name}]!";
                                    TShock.Utils.Broadcast(broadCastMsg, Color.Cyan);

                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine($"[Loot] {player.Name} obtained {item.Name}");
                                    Console.ResetColor();
                                }
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
