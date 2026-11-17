using System;
using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Multiplayer;
using UnityEngine;
using UnityEngine.SceneManagement;
using Random = UnityEngine.Random;

public class LobbyNetworking : MonoBehaviour
{
    public static LobbyNetworking Instance { get; private set; }
    [SerializeField] private UnityTransport _transport;
    // Constants
    private const int MAX_PLAYER_AMOUNT = 4;
    private const string GAME_SCENE_NAME = "VillageScene";

    // Player information
    public string PlayerName { get; private set; }
    public PlayerClass SelectedClass { get; private set; }
    public string JoinCode { get; private set; }
    public string LastError { get; private set; }
    // Player active session
    private ISession _currentSession;
    public ISession CurrentSession => _currentSession;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private async void Start()
    {
        NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

        // Needed for multiple conections
        InitializationOptions initializationOptions = new();
#if UNITY_EDITOR
        initializationOptions.SetProfile(Random.Range(0, 10000).ToString());
#endif

        await UnityServices.InitializeAsync(initializationOptions);

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();
    }

    private void OnDestroy()
    {
        if (NetworkManager.Singleton != null)
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
    }

    public async Task<string> CreateLobby(string playerName, PlayerClass selectedClass)
    {
        PlayerName = playerName;
        SelectedClass = selectedClass;
        LastError = null;

        var options = new SessionOptions
        {
            MaxPlayers = MAX_PLAYER_AMOUNT,
            IsPrivate = true
        }.WithRelayNetwork();

        try
        {
            _currentSession = await MultiplayerService.Instance.CreateSessionAsync(options);
            JoinCode = _currentSession.Code;

            NetworkManager.Singleton.SceneManager.LoadScene(GAME_SCENE_NAME, LoadSceneMode.Single);

            return JoinCode;
        }
        catch (Exception e)
        {
            LastError = e.Message;
            Debug.LogError($"An error has occurred: {e.Message}");
            return null;
        }
    }

    public async Task<bool> JoinLobby(string joinCode, string playerName, PlayerClass selectedClass)
    {
        PlayerName = playerName;
        SelectedClass = selectedClass;
        JoinCode = joinCode;
        LastError = null;

        try
        {
            _currentSession = await MultiplayerService.Instance.JoinSessionByCodeAsync(JoinCode);
            return true;
        }
        catch (SessionException e)
        {
            JoinCode = null;
            LastError = e.Message;
            Debug.LogWarning($"Connection failed: {e.Message}");
            return false;
        }
    }

    public async Task LeaveLobby()
    {
        if (_currentSession == null)
            return;

        ISession session = _currentSession;
        _currentSession = null;
        JoinCode = null;

        await session.LeaveAsync();
    }

    private void OnClientDisconnected(ulong clientId)
    {
        if (clientId != NetworkManager.Singleton.LocalClientId)
            return;

        _ = LeaveLobby();
    }
}
