using System.Collections;
using UnityEngine;

namespace Obrissom.UI
{
    public class DamagePopUpPoolBase : Pool<DamagePopUpAnimation, PopUpContext>
    {
        [SerializeField] private float _popUpDuration;

        public void CreatePopUp(Vector3 position, string text, bool critic)
        {
            Get(position, new PopUpContext { text = text, critic = critic });
        }

        protected override void InstantiateObject(DamagePopUpAnimation popUpAnim, Vector3 position, PopUpContext context)
        {
            GameObject popUp = popUpAnim.gameObject;
            popUp.transform.position = position + new Vector3(Random.Range(-0.3f, 0.3f), 0f, Random.Range(-0.3f, 0.3f));
            popUp.SetActive(true);
            popUpAnim.Init(context.text);

            StartCoroutine(ReturnAfterDelay(popUpAnim));
        }

        private IEnumerator ReturnAfterDelay(DamagePopUpAnimation popUp)
        {
            yield return new WaitForSeconds(_popUpDuration);
            Return(popUp);
        }
    }
}