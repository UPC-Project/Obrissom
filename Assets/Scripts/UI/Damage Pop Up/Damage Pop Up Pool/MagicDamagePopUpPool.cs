using Obrissom.UI;

public class MagicDamagePopUpPool : DamagePopUpPoolBase
{
    public static MagicDamagePopUpPool Instance { get; private set; }
    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }
}