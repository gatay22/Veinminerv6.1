using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace ChestSelfDestruct
{
    [ApiVersion(2, 1)]
    public class RandomLootPlugin : TerrariaPlugin
    {
        public override string Name => "Self-Destruct Random Chest";
        public override string Author => "Player";
        public override Version Version => new Version(1, 2, 0);

        // Pool item: Life Crystal, Bar, Potion, Rare Materials
        private int[] lootPool = { 29, 21, 19, 706, 31, 2353, 290, 292, 499, 1225 };

        public RandomLootPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            // Packet 31: Saat player berinteraksi dengan Chest
            if (args.MsgID != PacketTypes.ChestOpen) return;

            TSPlayer tsPlayer = TShock.Players[args.Msg.whoAmI];
            if (tsPlayer == null || !tsPlayer.Active) return;

            using (var reader = new System.IO.BinaryReader(new System.IO.MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                short x = reader.ReadInt16();
                short y = reader.ReadInt16();

                int chestID = Chest.FindChest(x, y);
                if (chestID != -1)
                {
                    // 1. Acak isi Chest khusus untuk player tersebut
                    RandomizeChest(chestID);

                    // 2. Efek Visual/Pesan (Self-Destruct Warning)
                    tsPlayer.SendMessage("PERINGATAN: Chest ini akan meledak/hancur dalam sekejap!", Color.Red);

                    // 3. Hancurkan Tile di Map (Self-Destruct)
                    // Menggunakan WorldGen.KillTile agar chest hilang dari map tanpa drop item chest-nya
                    WorldGen.KillTile(x, y, false, false, false);

                    // 4. Sinkronisasi ke semua player bahwa chest tersebut sudah hilang
                    NetMessage.SendData((int)PacketTypes.TileClean, -1, -1, null, x, y);
                    
                    // Tambahan efek suara ledakan di posisi chest (opsional secara visual server)
                    // Player akan melihat chest hilang dari hadapan mereka setelah dibuka.
                }
            }
        }

        private void RandomizeChest(int chestID)
        {
            Chest chest = Main.chest[chestID];
            if (chest == null) return;

            Random rand = new Random();

            for (int i = 0; i < 40; i++)
            {
                // Isi 4-8 slot secara acak
                if (i < rand.Next(4, 9))
                {
                    int itemID = lootPool[rand.Next(lootPool.Length)];
                    chest.item[i].SetDefaults(itemID);
                    chest.item[i].stack = rand.Next(1, 6);
                }
                else
                {
                    chest.item[i].SetDefaults(0);
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
