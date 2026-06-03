using Obrissom.UI;

public class PhyiscDamagePopUpPool : DamagePopUpPoolBase
{
    public static PhyiscDamagePopUpPool Instance { get; private set; }
    protected override void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        base.Awake();
    }
}