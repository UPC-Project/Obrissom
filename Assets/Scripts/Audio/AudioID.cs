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

        // Environment 300–399
        DoorOpen    = 300,
        ChestOpen   = 301,

        // UI 400–499
        UIClick         = 400,
        UIConfirm       = 401,
        UICancel        = 402,
        InventoryOpen   = 403,
        InventoryClose  = 404,
    }
}
