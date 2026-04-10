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
        public override Version Version => new Version(1, 0, 0);

        private int timer = 0;
        // 60 detik * 60 tick = 3600 per menit. 10 menit = 36000.
        private const int ClearInterval = 36000; 

        public ClearLag(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.GameUpdate.Register(this, OnUpdate);
            Commands.ChatCommands.Add(new Command("clearlag.admin", ForceClear, "clearlag") 
            { 
                HelpText = "Menghapus semua item yang tergeletak di tanah secara manual." 
            });
        }

        private void OnUpdate(EventArgs args)
        {
            timer++;

            // Peringatan 30 detik sebelum pembersihan
            if (timer == ClearInterval - 1800)
            {
                TSPlayer.All.SendInfoMessage("[ClearLag] Pengosongan sampah dalam 30 detik!");
            }

            // Eksekusi pembersihan otomatis
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
            for (int i = 0; i < Main.maxItems; i++)
            {
                if (Main.item[i].active)
                {
                    Main.item[i].active = false; // Hapus item
                    // Kirim data ke semua player agar item hilang di layar mereka
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
