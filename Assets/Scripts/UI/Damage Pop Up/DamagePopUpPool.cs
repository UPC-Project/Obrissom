using UnityEngine;

namespace Obrissom.UI
{
    public class DamagePopUpPool : MonoBehaviour
    {
        public static DamagePopUpPool Instance { get; private set; }
        public GameObject physicDamagePrefab;
        public GameObject magicDamagePrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
        }

        public void CreatePopUp(Vector3 position, string text, DamageType damageType, bool critic)
        {
            GameObject popUp = null;
            if (damageType == DamageType.PhysicDamage)
            {
                popUp = Instantiate(physicDamagePrefab, transform);
            }
            else if (damageType == DamageType.MagicDamage)
            {
                popUp = Instantiate(magicDamagePrefab, transform);
            }

            popUp.transform.position = position;
            var temp = popUp.GetComponent<DamagePopUpAnimation>();
            temp.damageText.text = text;

            Destroy(popUp, 1f);
        }
    }
}
