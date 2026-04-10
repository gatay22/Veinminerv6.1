using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

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
        if (args.MsgID != PacketTypes.TileManipulation) return;

        using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
        {
            byte action = reader.ReadByte();
            short x = reader.ReadInt16();
            short y = reader.ReadInt16();
            ushort type = reader.ReadUInt16();

            if (action == 0 && IsOre(Main.tile[x, y].type))
            {
                MineVein(x, y, Main.tile[x, y].type);
            }
        }
    }

    private bool IsOre(int type)
    {
        // Daftar ID Bijih (Copper, Iron, Gold, dll)
        int[] ores = { 7, 8, 9, 22, 25, 37, 48, 56, 107, 108, 111, 121, 166, 167, 168, 169 };
        return ores.Contains(type);
    }

    private void MineVein(int x, int y, int type)
    {
        // Loop simpel untuk hancurin blok sekitar
        for (int i = x - 1; i <= x + 1; i++)
        {
            for (int j = y - 1; j <= y + 1; j++)
            {
                if (Main.tile[i, j].active() && Main.tile[i, j].type == type)
                {
                    WorldGen.KillTile(i, j, false, false, false);
                    TSPlayer.All.SendTileSquare(i, j, 1);
                    // Catatan: Untuk skala besar butuh rekursi, tapi ini cukup untuk awal
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
