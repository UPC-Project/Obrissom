using System.Collections;
using UnityEngine;

namespace Obrissom.UI
{
    public abstract class InformationPopUp : MonoBehaviour
    {
        [SerializeField] protected float _showTime;
        [SerializeField] protected GameObject _popup;
        private static bool _isAnyPopupPlaying = false; // shared by all instances

        protected IEnumerator Show()
        {
            // wait for the currently playing popup to finish
            while (true)
            {
                if (!_isAnyPopupPlaying)
                {
                    _isAnyPopupPlaying = true;
                    break;
                }
                yield return null;
            }

            _popup.SetActive(true);
            yield return new WaitForSecondsRealtime(_showTime);
            _popup.SetActive(false);
            _isAnyPopupPlaying = false;
        }
    }
}
