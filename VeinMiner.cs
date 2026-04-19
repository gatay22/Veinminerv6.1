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
        public override string Description => "Broadcasts a message when a player finds rare loot.";
        public override Version Version => new Version(1, 0, 4);

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

                if (itemIndex >= 0 && itemIndex < Main.item.Length)
                {
                    // Ambil WorldItem dari array server
                    var worldItem = Main.item[itemIndex];

                    // Karena kita tidak bisa cast ke Item, kita buat instance baru 
                    // berdasarkan ID item tersebut untuk mengecek rarity-nya
                    Item tempItem = new Item();
                    tempItem.SetDefaults(worldItem.type);

                    if (tempItem != null && !string.IsNullOrEmpty(tempItem.Name))
                    {
                        // Check rarity: 4+ is rare, or special fishing items
                        if (tempItem.rare >= 4 || (tempItem.fishingPole > 0 && tempItem.rare >= 3) || tempItem.questItem)
                        {
                            int playerIndex = args.ignoreClient;
                            
                            if (playerIndex >= 0 && playerIndex < TShock.Players.Length)
                            {
                                TSPlayer player = TShock.Players[playerIndex];

                                if (player != null && player.Active)
                                {
                                    // Menggunakan Tag [i:ID] untuk memunculkan gambar item di chat
                                    string broadCastMsg = $"[c/00FFFF:RARE FIND!] [c/E1E1E1:{player.Name}] just found [i:{tempItem.type}] [c/FFD700:{tempItem.Name}]!";
                                    TShock.Utils.Broadcast(broadCastMsg, Color.Cyan);

                                    Console.ForegroundColor = ConsoleColor.Cyan;
                                    Console.WriteLine($"[Loot] {player.Name} obtained {tempItem.Name}");
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
