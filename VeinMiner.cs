using System;
using System.IO;
using System.Linq;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace MyVeinMiner
{
    [ApiVersion(2, 1)]
    public class VeinMiner : TerrariaPlugin
    {
        public override string Name => "Simple Vein Miner";
        public override string Author => "Player";
        public override Version Version => new Version(1, 0, 0);

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            // PERBAIKAN LAGI: Di TShock 6.1, namanya adalah PacketTypes.Tile
            if (args.MsgID != PacketTypes.Tile) return;

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                byte action = reader.ReadByte();
                short x = reader.ReadInt16();
                short y = reader.ReadInt16();
                ushort type = reader.ReadUInt16();

                if (action == 0) // Menghancurkan blok
                {
                    if (x < 0 || y < 0 || x >= Main.maxTilesX || y >= Main.maxTilesY) return;

                    var tile = Main.tile[x, y];
                    if (tile.active() && IsOre(tile.type))
                    {
                        MineVein(x, y, tile.type);
                    }
                }
            }
        }

        private bool IsOre(int type)
        {
            int[] ores = { 7, 8, 9, 22, 25, 37, 48, 56, 107, 108, 111, 121, 166, 167, 168, 169 };
            return ores.Contains(type);
        }

        private void MineVein(int x, int y, int type)
        {
            for (int i = x - 1; i <= x + 1; i++)
            {
                for (int j = y - 1; j <= y + 1; j++)
                {
                    if (i < 0 || j < 0 || i >= Main.maxTilesX || j >= Main.maxTilesY) continue;

                    if (Main.tile[i, j].active() && Main.tile[i, j].type == type)
                    {
                        WorldGen.KillTile(i, j, false, false, false);
                        
                        // PERBAIKAN WARNING: Kita gunakan SendTileSquare standar
                        // TShock 6.1 biasanya minta (x, y, size)
                        TSPlayer.All.SendTileSquare(i, j, 1);
                    }
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
