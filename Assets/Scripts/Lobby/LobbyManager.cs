using Unity.Cinemachine;
using UnityEngine;
using UnityEngine.Playables;

public class LobbyManager : MonoBehaviour
{
    public static LobbyManager Instance { get; private set; }

    [Header("Panels")]
    [SerializeField] private GameObject _mainMenuPannel;
    [SerializeField] private GameObject _characterSelectionPannel;
    [Header("Timeline Director")]
    [SerializeField] private PlayableDirector _director;
    [Header("Cameras")]
    [SerializeField] private CinemachineCamera _characterSelectionCamera;
    [SerializeField] private CinemachineCamera _townCamera;
    
    private bool _isHost;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        GoBackMainMenu();
    }

    public void Settings()
    {

    }

    public void ExitGame()
    {
        Application.Quit();
    }

    public void InitiateCharacterCreation(bool isHost)
    {
        _isHost = isHost;
        _mainMenuPannel.SetActive(false);
        _director.Play();
    }

    private void OnEnable()
    {
        _director.stopped += OnTimelineStopped;
    }

    private void OnDisable()
    {
        _director.stopped -= OnTimelineStopped;
    }

    private void OnTimelineStopped(PlayableDirector director)
    {
        _characterSelectionPannel.SetActive(true);
        CharacterSelection.Instance.SetCharacterSelection(_isHost);

        _characterSelectionCamera.Priority = 20;
        _townCamera.Priority = 10;
    }

    public void GoBackMainMenu()
    {
        _characterSelectionPannel.SetActive(false);
        _mainMenuPannel.SetActive(true);
        CharacterSelection.Instance.HideCharactersModels();

        _characterSelectionCamera.Priority = 10;
        _townCamera.Priority = 20;
    }
}
