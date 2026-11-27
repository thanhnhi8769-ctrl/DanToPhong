using System;
using System.Collections;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : MonoBehaviour
{
    [Header("Lobby UI")]
    public TMP_Text statusText;
    public TMP_InputField roomInput;
    public GameObject createButton;
    public GameObject joinButton;
    public GameObject startButton;

    [Header("Vehicle / Map Selection")]
    public GameObject selectButton;
    public string tenSceneSelect = "SelectScene";

    [Header("Copy Code Button")]
    public GameObject copyButton;

    private bool loadingGame;
    private Coroutine roomMonitor;

    public static void DanhDauCoSanPhongTuVanTruoc()
    {
    }

    private async void Start()
    {
        Time.timeScale = 1f;
        if (startButton != null)
            startButton.SetActive(false);
        if (selectButton != null)
            selectButton.SetActive(false);
        if (copyButton != null)
            copyButton.SetActive(false);

        if (!ServerClient.Ready && !await ServerClient.ConnectAsync())
        {
            SetStatus("Cannot connect to the TCP game server.");
            return;
        }

        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            string requestedRoom = LauncherArgs.Room;
            Resp state = await ServerClient.CallAsync(new Req { command = "ROOM_STATE" });
            if (state != null && state.success)
            {
                ApplyRoomState(state);
                if (state.isRacing)
                    LoadGame(state);
                else
                    roomMonitor = StartCoroutine(MonitorRoom());
                return;
            }
            LauncherArgs.Room = string.Empty;
            await JoinRoomAsync(requestedRoom);
            return;
        }

        SetStatus("Create a room or enter a room code to join.");
    }

    private void Update()
    {
        if (InputEscapeUtility.TryConsume())
        {
            if (HelpSystem.DongNeuDangMo() || MenuLeaderboard.DongNeuDangMo())
                return;
            BamNutExit();
        }
    }

    public async void CreateRoom()
    {
        if (!await EnsureConnected())
            return;

        Resp response = await ServerClient.CallAsync(new Req
        {
            command = "ROOM_CREATE",
            mapName = PlayerInventory.GetSelectedMap(),
            maxPlayers = 6
        });
        if (response == null || !response.success)
        {
            SetStatus(response != null ? response.message : "Room creation failed.");
            return;
        }

        ApplyRoomState(response);
        if (copyButton != null)
            copyButton.SetActive(true);
        roomMonitor = StartCoroutine(MonitorRoom());
    }

    public async void JoinRoom()
    {
        string code = roomInput != null ? roomInput.text.Trim() : string.Empty;
        await JoinRoomAsync(code);
    }

    public void VaoPhongTheoMa(string code)
    {
        if (roomInput != null)
            roomInput.text = code;
        _ = JoinRoomAsync(code);
    }

    private async System.Threading.Tasks.Task JoinRoomAsync(string code)
    {
        if (!await EnsureConnected())
            return;

        Resp response = await ServerClient.CallAsync(new Req
        {
            command = "ROOM_JOIN",
            roomId = code
        });
        if (response == null || !response.success)
        {
            Resp spectate = await ServerClient.CallAsync(new Req
            {
                command = "GAME_SPECTATE",
                roomId = code
            });
            if (spectate == null || !spectate.success)
            {
                SetStatus(spectate != null ? spectate.message : response != null ? response.message : "Joining room failed.");
                return;
            }
            response = spectate;
        }

        ApplyRoomState(response);
        roomMonitor = StartCoroutine(MonitorRoom());
    }

    public async void StartGame()
    {
        if (!await EnsureConnected())
            return;

        Resp response = await ServerClient.CallAsync(new Req
        {
            command = "ROOM_START",
            mapName = PlayerInventory.GetSelectedMap()
        });
        if (response == null || !response.success)
        {
            SetStatus(response != null ? response.message : "Could not start the game.");
            return;
        }
        ApplyRoomState(response);
        LoadGame(response);
    }

    public void MoSceneChonXe()
    {
        if (!Application.CanStreamedLevelBeLoaded(tenSceneSelect))
        {
            SetStatus("Vehicle selection scene is missing from Build Settings.");
            return;
        }
        SceneManager.LoadScene(tenSceneSelect);
    }

    public void CopyMaPhong()
    {
        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room))
            GUIUtility.systemCopyBuffer = LauncherArgs.Room;
    }

    public async void BamNutExit()
    {
        if (!string.IsNullOrWhiteSpace(LauncherArgs.Room) && ServerClient.Ready)
            await ServerClient.CallAsync(new Req { command = "ROOM_LEAVE" });
        LauncherArgs.Room = string.Empty;
        LauncherArgs.RoomMap = string.Empty;
        if (Application.CanStreamedLevelBeLoaded("menu"))
            SceneManager.LoadScene("menu");
        else
            Application.Quit();
    }

    private IEnumerator MonitorRoom()
    {
        var wait = new WaitForSecondsRealtime(1f);
        while (!loadingGame && !string.IsNullOrWhiteSpace(LauncherArgs.Room))
        {
            var task = ServerClient.CallAsync(new Req { command = "ROOM_STATE" });
            yield return new WaitUntil(() => task.IsCompleted);
            if (task.Status == System.Threading.Tasks.TaskStatus.RanToCompletion)
            {
                Resp response = task.Result;
                if (response != null && response.success)
                {
                    ApplyRoomState(response);
                    if (response.isRacing)
                    {
                        LoadGame(response);
                        yield break;
                    }
                }
                else if (response != null)
                {
                    SetStatus(response.message);
                }
            }
            yield return wait;
        }
    }

    private async System.Threading.Tasks.Task<bool> EnsureConnected()
    {
        if (ServerClient.Ready)
            return true;
        bool connected = await ServerClient.ConnectAsync();
        if (!connected)
            SetStatus("Cannot connect to the TCP game server.");
        return connected;
    }

    private void ApplyRoomState(Resp response)
    {
        LauncherArgs.Room = response.roomId ?? LauncherArgs.Room;
        LauncherArgs.RoomMap = response.mapName ?? PlayerInventory.GetSelectedMap();
        LauncherArgs.SpawnIndex = response.spawnIndex;
        LauncherArgs.IsRoomHost = response.isHost;
        LauncherArgs.IsSpectating = response.isSpectator;

        if (roomInput != null && !string.IsNullOrWhiteSpace(LauncherArgs.Room))
            roomInput.text = LauncherArgs.Room;
        if (startButton != null)
            startButton.SetActive(response.isHost && !response.isRacing);
        if (selectButton != null)
            selectButton.SetActive(response.isHost && !response.isRacing);
        if (copyButton != null)
            copyButton.SetActive(!string.IsNullOrWhiteSpace(LauncherArgs.Room));

        SetStatus(response.isSpectator
            ? $"Spectating room {LauncherArgs.Room}."
            : $"Room {LauncherArgs.Room}: {response.playerCount}/{response.maxPlayers} players" +
              (response.isHost ? " (host)" : string.Empty));
    }

    private void LoadGame(Resp response)
    {
        if (loadingGame)
            return;
        loadingGame = true;
        if (roomMonitor != null)
            StopCoroutine(roomMonitor);

        LauncherArgs.Mode = "multiplayer";
        LauncherArgs.RoomMap = response.mapName ?? LauncherArgs.RoomMap;
        string scene = GameNetworkManager.LayTenSceneVaoGame();
        if (!Application.CanStreamedLevelBeLoaded(scene))
        {
            loadingGame = false;
            SetStatus($"Selected track scene '{scene}' is not in Build Settings.");
            return;
        }
        SceneManager.LoadScene(scene);
    }

    private void SetStatus(string message)
    {
        if (statusText != null)
            statusText.text = message ?? string.Empty;
        Debug.Log("[TCP lobby] " + (message ?? string.Empty));
    }
}
