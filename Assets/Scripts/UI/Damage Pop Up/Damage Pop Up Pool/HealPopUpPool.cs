using Obrissom.UI;

public class HealPopUpPool : DamagePopUpPoolBase
{
    public static HealPopUpPool Instance { get; private set; }
    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }
}
