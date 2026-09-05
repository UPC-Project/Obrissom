using System.Collections;
using UnityEngine;

namespace Obrissom.UI
{
    public abstract class InformationPopUp : MonoBehaviour
    {
        [SerializeField] protected float _showTime;
        [SerializeField] protected GameObject _popup;

        protected IEnumerator Show()
        {
            _popup.SetActive(true);
            yield return new WaitForSecondsRealtime(_showTime);
            _popup.SetActive(false);
        }
    }
}
