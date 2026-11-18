using System.Collections;
using UnityEngine;

public class CopyToClipboardPopup : MonoBehaviour
{
    [SerializeField] private RectTransform _popupRect;
    [SerializeField] private float _slideDistance = 40f;
    [SerializeField] private float _popupDuration = 0.6f;

    private Vector2 _startPos;
    private Coroutine _animationCoroutine;

    private void Awake()
    {
        _startPos = _popupRect.anchoredPosition;
        _popupRect.gameObject.SetActive(false);
    }

    public void CopyJoinCodeToClipboard()
    {
        CopyToClipboard(LobbyNetworking.Instance.JoinCode);
    }

    public void CopyToClipboard(string text)
    {
        GUIUtility.systemCopyBuffer = text;

        if (_animationCoroutine != null) StopCoroutine(_animationCoroutine);

        _animationCoroutine = StartCoroutine(PlayPopupAnimation());
    }

    private IEnumerator PlayPopupAnimation()
    {
        _popupRect.gameObject.SetActive(true);
        _popupRect.anchoredPosition = _startPos;

        Vector2 targetPos = _startPos + Vector2.up * _slideDistance;
        float elapsed = 0f;

        while (elapsed < _popupDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / _popupDuration);
            _popupRect.anchoredPosition = Vector2.Lerp(_startPos, targetPos, t);
            yield return null;
        }

        _popupRect.gameObject.SetActive(false);
        _animationCoroutine = null;
    }
}
