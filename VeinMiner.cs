using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace NaturalChestOnly
{
    [ApiVersion(2, 1)]
    public class RandomLootPlugin : TerrariaPlugin
    {
        public override string Name => "Natural Self-Destruct Chest";
        public override string Author => "Player";
        public override Version Version => new Version(1, 3, 0);

        private int[] lootPool = { 29, 21, 19, 706, 31, 2353, 290, 292, 499, 1225 };

        public RandomLootPlugin(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
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
                    // CEK APAKAH INI CHEST ALAMI
                    // frameX > 36 biasanya berarti ini bukan chest kayu standar buatan player
                    // Atau kita cek tipe chest (Gold, Ice, Shadow, dll)
                    ushort tileType = Main.tile[x, y].type;
                    short frameX = Main.tile[x, y].frameX;

                    // Jika ini Chest Kayu Biasa (frameX == 0 sampai 35) dan ditaruh di area rumah (Surface), skip.
                    // Kebanyakan chest alami (Gold, Ivy, Frozen) punya frameX 36 ke atas.
                    if (frameX == 0 && x > Main.spawnTileX - 100 && x < Main.spawnTileX + 100) 
                    {
                        return; // Abaikan jika ini kemungkinan chest player di sekitar spawn
                    }

                    // Eksekusi hanya untuk chest yang bukan chest kayu biasa buatan player
                    if (frameX > 0 || tileType != 21) 
                    {
                        RandomizeChest(chestID);
                        tsPlayer.SendMessage("Chest ALAMI terdeteksi! Segera ambil isinya!", Color.Orange);
                        
                        WorldGen.KillTile(x, y, false, false, false);
                        NetMessage.SendData((int)PacketTypes.TileClean, -1, -1, null, x, y);
                    }
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
                if (i < rand.Next(4, 9))
                {
                    chest.item[i].SetDefaults(lootPool[rand.Next(lootPool.Length)]);
                    chest.item[i].stack = rand.Next(1, 6);
                }
                else chest.item[i].SetDefaults(0);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
