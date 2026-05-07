using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace Obrissom.UI
{
    public class DamagePopUpPool : MonoBehaviour
    {
        public static DamagePopUpPool Instance { get; private set; }
        public GameObject physicDamagePrefab;
        public GameObject magicDamagePrefab;
        [SerializeField] private int initialPoolSize = 10;

        private Queue<GameObject> _physicPool = new Queue<GameObject>();
        private Queue<GameObject> _magicPool = new Queue<GameObject>();

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            for (int i = 0; i < initialPoolSize; i++)
            {
                _physicPool.Enqueue(CreateNew(physicDamagePrefab));
                _magicPool.Enqueue(CreateNew(magicDamagePrefab));
            }
        }

        private GameObject CreateNew(GameObject prefab)
        {
            var obj = Instantiate(prefab, transform);
            obj.SetActive(false);
            return obj;
        }

        private GameObject GetFromPool(Queue<GameObject> pool, GameObject prefab)
        {
            if (pool.Count > 0)
            {
                var obj = pool.Dequeue();
                obj.SetActive(true);
                return obj;
            }
            var newObj = Instantiate(prefab, transform);
            return newObj;
        }

        public void ReturnToPool(GameObject popUp, DamageType damageType)
        {
            popUp.SetActive(false);
            if (damageType == DamageType.PhysicDamage)
                _physicPool.Enqueue(popUp);
            else if (damageType == DamageType.MagicDamage)
                _magicPool.Enqueue(popUp);
        }

        public void CreatePopUp(Vector3 position, string text, DamageType damageType, bool critic)
        {
            Queue<GameObject> pool = damageType == DamageType.PhysicDamage ? _physicPool : _magicPool;
            GameObject prefab = damageType == DamageType.PhysicDamage ? physicDamagePrefab : magicDamagePrefab;
            GameObject popUp = GetFromPool(pool, prefab);

            position += new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            popUp.transform.position = position;
            var anim = popUp.GetComponent<DamagePopUpAnimation>();
            anim.Init(text);

            StartCoroutine(ReturnAfterDelay(popUp, damageType, anim.displayDuration));
        }

        private IEnumerator ReturnAfterDelay(GameObject popUp, DamageType damageType, float delay)
        {
            yield return new WaitForSeconds(delay);
            ReturnToPool(popUp, damageType);
        }
    }
}
