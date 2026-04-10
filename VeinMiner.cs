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
        public override Version Version => new Version(1, 3, 2);

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
                    // Ambil frameX untuk cek apakah ini chest alami
                    short frameX = Main.tile[x, y].frameX;

                    // frameX >= 36 adalah chest selain chest kayu standar (alami)
                    if
