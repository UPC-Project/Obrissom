namespace Obrissom.Audio
{
    /// <summary>
    /// Identifier for every sound in the game. Used from code and sent over the network as an int.
    /// Values are explicit and grouped in blocks of 100: never renumber an existing entry,
    /// SoundLibrary assets store the number, not the name.
    /// </summary>
    public enum AudioID
    {
        None = 0,

        // Player 100–199
        PlayerFootstep = 100,
        PlayerJump     = 101,
        PlayerHurt     = 102,

        // Enemy 200–299
        SlasherAttack = 200,
        SlasherHurt   = 201,
        SlasherDeath  = 202,

        PicozapatoBasicAttack = 210,
        PicozapatoDeath       = 211,
        PicozapatoAreaWindup  = 212,
        PicozapatoAreaImpact  = 213,

        // Environment 300–399
        DoorOpen    = 300,
        ChestOpen   = 301,

        // UI 400–499
        UIClick         = 400,
        UIConfirm       = 401,
        UICancel        = 402,
        InventoryOpen   = 403,
        InventoryClose  = 404,
        UIHover         = 405,

        // Skills 500–599
        ScytheCutCast       = 500,
        MagicProjectileCast = 501,
        TeleportCast        = 502,
        InfuseScytheCast    = 503,
        DestructionRingCast = 504,
        SwordCutCast        = 510,
        BugBuzzCast         = 511,
        HitPhysical         = 550,
        HitMagic            = 551,

        // Items 600–699
        ItemPickup = 600,
        Equip      = 601,
        Unequip    = 602,

        // Progress 700–799
        LevelUp       = 700,
        QuestAccepted = 701,
    }
}
