using System;
using System.IO;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace NaturalChestOnlyFixed
{
    [ApiVersion(2, 1)]
    public class RandomLootPlugin : TerrariaPlugin
    {
        public override string Name => "Natural Self-Destruct Chest Fixed";
        public override string Author => "Player";
        public override Version Version => new Version(1, 3, 3);

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

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                short x = reader.ReadInt16();
                short y = reader.ReadInt16();

                int chestID = Chest.FindChest(x, y);
                if (chestID != -1)
                {
                    short frameX = Main.tile[x, y].frameX;

                    // frameX >= 36 adalah chest alami
                    if (frameX >= 36) 
                    {
                        RandomizeChest(chestID);
                        tsPlayer.SendMessage("Chest ALAMI terdeteksi! Ambil isinya sebelum hancur!", Color.Orange);
                        
                        WorldGen.KillTile(x, y, false, false, false);
                        
                        // Versi kirim paket paling aman dan simpel
                        NetMessage.SendTileSquare(-1, x, y, 3);
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
