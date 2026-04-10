using System;
using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;

namespace ClearLagPlugin
{
    [ApiVersion(2, 1)]
    public class ClearLag : TerrariaPlugin
    {
        public override string Name => "Professional Clear Lag";
        public override string Author => "Gemini";
        public override Version Version => new Version(1, 0, 2);

        private int timer = 0;
        private const int ClearInterval = 36000; // 10 Menit

        public ClearLag(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            Commands.ChatCommands.Add(new Command("clearlag.admin", ForceClear, "clearlag"));
        }

        private void OnUpdate(EventArgs args)
        {
            timer++;

            if (timer == ClearInterval - 1800)
            {
                TSPlayer.All.SendInfoMessage("[ClearLag] Pengosongan sampah dalam 30 detik!");
            }

            if (timer >= ClearInterval)
            {
                ExecuteClear();
                timer = 0;
            }
        }

        private void ForceClear(CommandArgs args)
        {
            ExecuteClear();
            args.Player.SendSuccessMessage("Berhasil membersihkan sampah secara manual!");
        }

        private void ExecuteClear()
        {
            int count = 0;
            // Loop melalui semua slot item yang ada di dunia (maksimal 400)
            for (int i = 0; i < 400; i++)
            {
                // Kita cek tipenya, jika bukan 0 (udara) berarti ada itemnya
                if (Main.item[i] != null && Main.item[i].type != 0)
                {
                    // SetDefaults(0) akan menghapus data item dan menonaktifkannya secara internal
                    Main.item[i].SetDefaults(0);
                    
                    // Beritahu semua client bahwa item di indeks ini sekarang kosong
                    NetMessage.SendData((int)PacketTypes.ItemDrop, -1, -1, null, i);
                    count++;
                }
            }
            TSPlayer.All.SendSuccessMessage($"[ClearLag] Berhasil menghapus {count} item sampah di tanah!");
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.GameUpdate.Deregister(this, OnUpdate);
            base.Dispose(disposing);
        }
    }
}
