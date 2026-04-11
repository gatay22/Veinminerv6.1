private void ApplyHomingEffect(NPC primaryTarget, TSPlayer player)
{
    // Cari musuh terdekat selain yang baru saja dipukul
    NPC closestNpc = null;
    float closestDist = 400f; // Jarak maksimal pengejaran (25 blok)

    foreach (NPC npc in Main.npc)
    {
        if (npc.active && !npc.friendly && npc.whoAmI != primaryTarget.whoAmI && !npc.dontTakeDamage)
        {
            float dist = Vector2.Distance(primaryTarget.Center, npc.Center);
            if (dist < closestDist)
            {
                closestDist = dist;
                closestNpc = npc;
            }
        }
    }

    // Jika musuh kedua ditemukan, buat "energi" yang mengejar
    if (closestNpc != null)
    {
        // Visual: Gunakan partikel yang bergerak dari target A ke target B
        Vector2 startPos = primaryTarget.Center;
        Vector2 targetPos = closestNpc.Center;
        
        // Kita buat simulasi perjalanan partikel (Garis yang mengejar)
        for (int i = 0; i < 5; i++) 
        {
            // Lerp untuk membuat titik-titik di antara dua musuh
            float progress = i / 5f;
            Vector2 dustPos = Vector2.Lerp(startPos, targetPos, progress);
            
            // Berikan sedikit variasi posisi (biar gak lurus kaku)
            dustPos += new Vector2(Main.rand.Next(-5, 6), Main.rand.Next(-5, 6));

            int d = Dust.NewDust(dustPos, 1, 1, 226, 0, 0, 100, default, 0.8f); // 226: Biru Azure (Electric)
            Main.dust[d].noGravity = true;
            Main.dust[d].velocity *= 0.5f;
        }

        // Berikan damage ke musuh yang dikejar
        // Tanpa knockback supaya musuh gak terpental jauh dari jangkauan pedang
        player.TPlayer.ApplyDamageToNPC(closestNpc, player.TPlayer.HeldItem.damage / 3, 0f, 0, false);
        
        // Munculkan teks kecil di target yang kena kejar
        CombatText.NewText(closestNpc.getRect(), Color.Cyan, "Chained!", false, true);
    }
}
