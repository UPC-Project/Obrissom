using TMPro;
using Unity.Netcode;
using UnityEngine;

public class ServerTestingUI : MonoBehaviour
{
    [SerializeField] private GameObject _buttons;
    [SerializeField] private TextMeshProUGUI _text;
    public void StartHost()
    {
        NetworkManager.Singleton.StartHost();
        _buttons.SetActive(false);
        _text.text = "Host";
    }

    public void StartClient()
    {
        NetworkManager.Singleton.StartClient();
        _buttons.SetActive(false);
        _text.text = "Client";
    }
}
