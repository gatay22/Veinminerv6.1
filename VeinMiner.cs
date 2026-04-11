using System;
using System.Collections.Generic;
using Terraria;
using Terraria.ID;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System.IO;

namespace VeinMinerV6
{
    [ApiVersion(2, 1)]
    public class VeinMiner : TerrariaPlugin
    {
        public override string Name => "VeinMiner Final";
        public override string Author => "Gemini";
        public override Version Version => new Version(6, 1, 2);

        public VeinMiner(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            // Kita pakai angka 17 (ID paket Tile Edit di Terraria) 
            // Ini jauh lebih aman daripada pakai nama PacketTypes yang sering berubah
            if ((int)args.MsgID == 17)
            {
                using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
                {
                    byte action = reader.ReadByte();
                    int x = reader.ReadInt16();
                    int y = reader.ReadInt16();

                    // Action 1 = Player menghancurkan blok
                    if (action == 1)
                    {
                        TSPlayer player = TShock.Players[args.Msg.whoAmI];
                        if (player == null || !player.Active) return;

                        Item tool = player.TPlayer.HeldItem;

                        // Cek apakah pemain memegang Pickaxe atau Drill
                        if (tool.pick > 0)
                        {
                            ITile tile = Main.tile[x, y];
                            
                            // Pastikan yang dihancurkan adalah ORE
                            if (TileID.Sets.Ore[tile.type])
                            {
                                DestroyVein(x, y, tile.type, player);
                            }
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
            int maxBlocks = 150; 

            while (nodes.Count > 0 && count < maxBlocks)
            {
                Point current = nodes.Dequeue();

                if (current.X < 10 || current.X >= Main.maxTilesX - 10 || current.Y < 10 || current.Y >= Main.maxTilesY - 10) continue;
                if (visited.Contains(current)) continue;

                visited.Add(current);
                ITile tile = Main.tile[current.X, current.Y];

                if (tile.active() && tile.type == tileType)
                {
                    count++;
                    
                    // Hancurkan tile dan jatuhkan item (logika worldgen)
                    WorldGen.KillTile(current.X, current.Y, false, false, false);
                    
                    // Kirim pembaruan ke semua client agar sinkron
                    // Kita pakai ID 17 langsung di sini juga
                    NetMessage.SendData(17, -1, -1, null, 1, current.X, current.Y);

                    // Cek blok tetangga (atas, bawah, kiri, kanan)
                    nodes.Enqueue(new Point(current.X + 1, current.Y));
                    nodes.Enqueue(new Point(current.X - 1, current.Y));
                    nodes.Enqueue(new Point(current.X, current.Y + 1));
                    nodes.Enqueue(new Point(current.X, current.Y - 1));
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
