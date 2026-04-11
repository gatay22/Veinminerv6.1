using System;
using System.Collections.Generic;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace VeinMinerV6
{
    [ApiVersion(2, 1)]
    public class VeinMiner : TerrariaPlugin
    {
        public override string Name => "VeinMiner All-Ore";
        public override string Author => "Gemini";
        public override Version Version => new Version(6, 1, 0);

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            // Hook saat tile dihancurkan
            ServerApi.Hooks.NetSendData.Register(this, OnSendData);
        }

        private void OnSendData(SendDataEventArgs args)
        {
            // Cek paket Tile Break (Packet ID: 17)
            if (args.MsgId == PacketTypes.TileBreak)
            {
                int action = args.number; // 0 = Tile Break
                int x = args.number2;
                int y = args.number3;

                if (action == 0 && x >= 0 && y >= 0 && x < Main.maxTilesX && y < Main.maxTilesY)
                {
                    TSPlayer player = TShock.Players[args.ignoreClient];
                    if (player == null || !player.Active) return;

                    Item tool = player.TPlayer.HeldItem;

                    // SYARAT: Harus pakai Pickaxe atau Drill
                    if (tool.pick > 0 || tool.axe > 0) 
                    {
                        Tile tile = Main.tile[x, y];
                        
                        // Cek apakah yang dihancurkan adalah ORE (Semua jenis Ore)
                        if (TileID.Sets.Ore[tile.type])
                        {
                            DestroyVein(x, y, tile.type, player);
                        }
                    }
                }
            }
        }

        private void DestroyVein(int x, int y, ushort tileType, TSPlayer player)
        {
            Queue<Point> nodes = new Queue<Point>();
            nodes.Enqueue(new Point(x, y));

            HashSet<Point> visited = new HashSet<Point>();
            int count = 0;
            int maxBlocks = 100; // Batas blok sekali hancur (biar gak lag)

            while (nodes.Count > 0 && count < maxBlocks)
            {
                Point current = nodes.Dequeue();

                if (current.X < 0 || current.X >= Main.maxTilesX || current.Y < 0 || current.Y >= Main.maxTilesY) continue;
                if (visited.Contains(current)) continue;

                visited.Add(current);
                Tile tile = Main.tile[current.X, current.Y];

                // Jika tipe tile sama, hancurkan
                if (tile.active() && tile.type == tileType)
                {
                    count++;
                    
                    // Paksa hancur dan jatuhkan item
                    WorldGen.KillTile(current.X, current.Y, false, false, false);
                    
                    // Beritahu client kalau tile sudah hilang
                    TSPlayer.All.SendTileSquare(current.X, current.Y, 1);

                    // Cek tetangga (Atas, Bawah, Kiri, Kanan)
                    nodes.Enqueue(new Point(current.X + 1, current.Y));
                    nodes.Enqueue(new Point(current.X - 1, current.Y));
                    nodes.Enqueue(new Point(current.X, current.Y + 1));
                    nodes.Enqueue(new Point(current.X, current.Y - 1));
                }
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetSendData.Deregister(this, OnSendData);
            base.Dispose(disposing);
        }
    }
}
