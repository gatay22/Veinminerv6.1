using System;
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
        public override Version Version => new Version(1, 3, 1);

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
                    // Ambil data tile chest
                    Tile tile = Main.tile[x, y];
                    short frameX = tile.frameX;

                    // LOGIKA: Hanya proses jika bukan Chest Kayu Biasa (frameX 0-35)
                    // Chest alami (Gold, Ice, dll) selalu mulai dari frameX 36 ke atas.
                    if (frameX >= 36) 
                    {
                        RandomizeChest(chestID);
                        tsPlayer.SendMessage("Chest ALAMI terdeteksi! Segera ambil isinya!", Color.Orange);
                        
                        // Hancurkan tile chest di server
                        WorldGen.KillTile(x, y, false, false, false);
                        
                        // Kirim update ke semua player (Paket ID 17 = Tile)
                        // 0 = Action (Kill), x, y, 0, 0
                        NetMessage.SendData((int)PacketTypes.Tile, -1, -1, null, 0, x, y, 0, 0);
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
                    chest.item[i].SetDefaults(lootPool[rand.Next(loot
