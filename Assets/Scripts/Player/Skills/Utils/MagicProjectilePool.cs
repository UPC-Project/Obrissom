public class MagicProjectilePool : Pool<ProjectileTrigger, NoContext>
{
    public static MagicProjectilePool Instance { get; private set; }

    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }
}

public struct NoContext { }