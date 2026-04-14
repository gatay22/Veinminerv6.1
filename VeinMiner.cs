using Terraria;
using TerrariaApi.Server;
using TShockAPI;
using Microsoft.Xna.Framework;
using System.IO;
using System.Collections.Generic;

namespace MageReworkPlugin
{
    [ApiVersion(2, 1)]
    public class MageRework : TerrariaPlugin
    {
        public override string Name => "Mage Color Matcher";
        public override string Author => "Gemini AI";
        public override Version Version => new Version(1, 6, 0);

        private Dictionary<int, long> _lastExtraProjTime = new Dictionary<int, long>();

        public MageRework(Main game) : base(game) { }

        public override void Initialize()
        {
            ServerApi.Hooks.NetGetData.Register(this, OnGetData);
        }

        private void OnGetData(GetDataEventArgs args)
        {
            if (args.MsgID != PacketTypes.ProjectileNew) return;

            using (var reader = new BinaryReader(new MemoryStream(args.Msg.readBuffer, args.Index, args.Length)))
            {
                reader.ReadInt16(); // identity
                float posX = reader.ReadSingle();
                float posY = reader.ReadSingle();
                float vX = reader.ReadSingle();
                float vY = reader.ReadSingle();
                byte owner = reader.ReadByte();
                short type = reader.ReadInt16();

                TSPlayer player = TShock.Players[owner];
                if (player == null || !player.Active || player.SelectedItem == null || !player.SelectedItem.magic) return;

                // Anti-Spam Cooldown
                long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                if (!_lastExtraProjTime.ContainsKey(owner)) _lastExtraProjTime[owner] = 0;
                if (currentTime - _lastExtraProjTime[owner] < 280) return;
                _lastExtraProjTime[owner] = currentTime;

                // --- LOGIKA WARNA MENGIKUTI SENJATA ---
                int extraType = 122; // Default: Biru (Mana Bolt)
                string name = player.SelectedItem.Name.ToLower();

                if (name.Contains("ruby") || name.Contains("sparking") || name.Contains("fire"))
                    extraType = 506; // Merah (Flare)
                else if (name.Contains("emerald") || name.Contains("vile") || name.Contains("grass"))
                    extraType = 505; // Hijau (Flare)
                else if (name.Contains("diamond") || name.Contains("frost") || name.Contains("ice"))
                    extraType = 504; // Biru Muda/Putih (Flare)
                else if (name.Contains("amber") || name.Contains("topaz") || name.Contains("sand"))
                    extraType = 507; // Kuning/Oranye (Flare)
                else if (name.Contains("amethyst") || name.Contains("shadow") || name.Contains("vile"))
                    extraType = 521; // Ungu (Shadow Spark)
                else if (name.Contains("crimson") || name.Contains("blood"))
                    extraType = 181; // Merah Darah
                
                // Gunakan ID asli senjata untuk "Echo" jika tipe buku
                if (name.Contains("book")) extraType = type;

                // --- EKSEKUSI HOMING ---
                Vector2 velocity = new Vector2(vX, vY);
                NPC target = FindClosestNPC(posX, posY, 550f);
                if (target != null)
                {
                    Vector2 direction = target.Center - new Vector2(posX, posY);
                    direction.Normalize();
                    velocity = direction * 10f;
                }

                int p = Projectile.NewProjectile(null, posX, posY, velocity.X, velocity.Y, extraType, 
                    (int)(player.SelectedItem.damage * 0.4f), 1f, owner);
                
                NetMessage.SendData((int)PacketTypes.ProjectileNew, -1, -1, null, p);
            }
        }

        private NPC FindClosestNPC(float x, float y, float maxDist)
        {
            NPC closest = null;
            float minDist = maxDist;
            foreach (NPC npc in Main.npc)
            {
                if (npc != null && npc.active && !npc.friendly && npc.lifeMax > 5 && !npc.dontTakeDamage)
                {
                    float dist = Vector2.Distance(new Vector2(x, y), npc.Center);
                    if (dist < minDist) { minDist = dist; closest = npc; }
                }
            }
            return closest;
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) ServerApi.Hooks.NetGetData.Deregister(this, OnGetData);
            base.Dispose(disposing);
        }
    }
}
