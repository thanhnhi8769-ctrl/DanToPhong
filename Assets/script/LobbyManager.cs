using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

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
    public string tenSceneSelect = SceneNames.Select;

    [Header("Copy Code Button")]
    public GameObject copyButton;

    private bool loadingGame;
    private bool roomActionInProgress;
    private bool canStart;
    private long lastRoomEventId;
    private CancellationTokenSource roomMonitorCts;
    private GameObject roomActionsRoot;
    private TMP_Text roomCodeLabel;
    private GameObject roomRosterRoot;
    private readonly Dictionary<int, Sprite> roomAvatarSprites = new Dictionary<int, Sprite>();
    private readonly Dictionary<int, Texture2D> roomAvatarTextures = new Dictionary<int, Texture2D>();
    private readonly HashSet<int> pendingAvatarRequests = new HashSet<int>();
    private readonly HashSet<int> requestedAvatarIds = new HashSet<int>();
    private readonly List<TMP_Text> roomPlayerLabels = new List<TMP_Text>();
    private readonly List<Image> roomPlayerAvatars = new List<Image>();

    public static void DanhDauCoSanPhongTuVanTruoc()
    {
    }

    private async void Start()
    {
        Time.timeScale = 1f;
        CreateRoomActionUi();
        CreateRoomRosterUi();
        if (startButton != null)
            startButton.SetActive(false);
        if (selectButton != null)
            selectButton.SetActive(false);
        if (copyButton != null)
            copyButton.SetActive(false);
        SetRoomEntryVisible(true);

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
                if (state.isRacing && !state.isReturnedToLobby)
                    LoadGame(state);
                else
                    StartRoomMonitor();
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
        if (!TryBeginRoomAction())
            return;
        try
        {
            if (!await EnsureConnected())
                return;

            Resp response = await ServerClient.CallAsync(new Req
            {
                command = "ROOM_CREATE",
                mapName = PlayerInventory.GetSelectedMap(),
                maxPlayers = 4
            });
            if (response == null || !response.success)
            {
                SetStatus(response != null ? response.message : "Room creation failed.");
                return;
            }

            ApplyRoomState(response);
            if (copyButton != null)
                copyButton.SetActive(true);
            StartRoomMonitor();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus("Room creation failed because of a connection error.");
        }
        finally
        {
            EndRoomAction();
        }
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
        if (!TryBeginRoomAction())
            return;
        try
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
            StartRoomMonitor();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus("Joining the room failed because of a connection error.");
        }
        finally
        {
            EndRoomAction();
        }
    }

    public async void StartGame()
    {
        if (!canStart)
        {
            SetStatus("Start is available after every player returns to the lobby and is ready.");
            return;
        }
        if (!TryBeginRoomAction())
            return;
        try
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
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus("Could not start the game because of a connection error.");
        }
        finally
        {
            EndRoomAction();
        }
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
        if (!TryBeginRoomAction())
            return;
        try
        {
            if (!string.IsNullOrWhiteSpace(LauncherArgs.Room) && ServerClient.Ready)
                await ServerClient.CallAsync(new Req { command = "ROOM_LEAVE" });
            LauncherArgs.Room = string.Empty;
            LauncherArgs.RoomMap = string.Empty;
            if (Application.CanStreamedLevelBeLoaded("menu"))
                SceneManager.LoadScene(SceneNames.Menu);
            else
                Application.Quit();
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            SetStatus("Leaving the room failed because of a connection error.");
        }
        finally
        {
            EndRoomAction();
        }
    }

    private void StartRoomMonitor()
    {
        StopRoomMonitor();
        roomMonitorCts = new CancellationTokenSource();
        _ = MonitorRoomAsync(roomMonitorCts);
    }

    private async Task MonitorRoomAsync(CancellationTokenSource monitorCts)
    {
        CancellationToken token = monitorCts.Token;
        try
        {
            while (!loadingGame && !string.IsNullOrWhiteSpace(LauncherArgs.Room) && !token.IsCancellationRequested)
            {
                Resp response = await ServerClient.CallAsync(new Req { command = "ROOM_STATE" });
                if (response != null && response.success)
                {
                    ApplyRoomState(response);
                    if (response.isRacing && !response.isReturnedToLobby)
                    {
                        if (!response.isHost)
                        {
                            SetStatus("The host started the race. Loading the selected track...");
                            await Task.Delay(TimeSpan.FromMilliseconds(1400), token);
                        }
                        LoadGame(response);
                        return;
                    }
                }
                else if (response != null)
                {
                    SetStatus(response.message);
                }
                else
                {
                    SetStatus("Room status could not be refreshed; check the server connection.");
                }

                await Task.Delay(TimeSpan.FromSeconds(1), token);
            }
        }
        catch (OperationCanceledException) when (token.IsCancellationRequested)
        {
        }
        catch (Exception ex)
        {
            Debug.LogException(ex);
            if (!loadingGame && !token.IsCancellationRequested)
                SetStatus("Room status monitoring stopped because of a connection error.");
        }
        finally
        {
            if (ReferenceEquals(roomMonitorCts, monitorCts))
                roomMonitorCts = null;
            monitorCts.Dispose();
        }
    }

    private void StopRoomMonitor()
    {
        CancellationTokenSource monitorCts = roomMonitorCts;
        roomMonitorCts = null;
        monitorCts?.Cancel();
    }

    private bool TryBeginRoomAction()
    {
        if (roomActionInProgress)
            return false;
        roomActionInProgress = true;
        SetRoomButtonsInteractable(false);
        return true;
    }

    private void EndRoomAction()
    {
        roomActionInProgress = false;
        SetRoomButtonsInteractable(true);
    }

    private void SetRoomButtonsInteractable(bool interactable)
    {
        foreach (GameObject buttonObject in new[] { createButton, joinButton, selectButton })
        {
            if (buttonObject == null)
                continue;
            Button button = buttonObject.GetComponentInChildren<Button>();
            if (button != null)
                button.interactable = interactable;
        }
        if (startButton != null)
        {
            Button button = startButton.GetComponentInChildren<Button>();
            if (button != null)
                button.interactable = interactable && canStart;
        }
    }

    private void OnDestroy()
    {
        StopRoomMonitor();
        foreach (Sprite sprite in roomAvatarSprites.Values)
            if (sprite != null) Destroy(sprite);
        foreach (Texture2D texture in roomAvatarTextures.Values)
            if (texture != null) Destroy(texture);
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
        string previousRoom = LauncherArgs.Room;
        LauncherArgs.Room = response.roomId ?? LauncherArgs.Room;
        LauncherArgs.RoomMap = response.mapName ?? PlayerInventory.GetSelectedMap();
        LauncherArgs.SpawnIndex = response.spawnIndex;
        LauncherArgs.IsRoomHost = response.isHost;
        LauncherArgs.IsSpectating = response.isSpectator;
        if (!string.Equals(previousRoom, LauncherArgs.Room, StringComparison.OrdinalIgnoreCase))
            lastRoomEventId = 0;

        if (roomInput != null && !string.IsNullOrWhiteSpace(LauncherArgs.Room))
            roomInput.text = LauncherArgs.Room;
        bool inRoom = !string.IsNullOrWhiteSpace(LauncherArgs.Room);
        canStart = response.canStart;
        if (startButton != null)
            startButton.SetActive(response.isHost && !response.isRacing);
        if (selectButton != null)
            selectButton.SetActive(!response.isRacing && !response.isSpectator);
        if (copyButton != null)
            copyButton.SetActive(inRoom);
        if (roomActionsRoot != null)
            roomActionsRoot.SetActive(inRoom);
        if (roomRosterRoot != null)
            roomRosterRoot.SetActive(inRoom);
        if (roomCodeLabel != null)
            roomCodeLabel.text = $"ROOM {LauncherArgs.Room}  |  ID {GetMyPlayerId()}";
        UpdateRoomRoster(response.roomPlayers);
        SetRoomEntryVisible(!inRoom);

        if (response.isSpectator)
            SetStatus($"Spectating room {LauncherArgs.Room}.");
        else if (response.isRacing && response.isReturnedToLobby)
            SetStatus($"You are back in room {LauncherArgs.Room}. Waiting for all players to return to the lobby.");
        else if (response.isRacing)
            SetStatus(response.isHost ? "Race started. Loading the selected track..." : "The host started the race. Joining the track...");
        else if (response.unreadyPlayerCount > 0)
            SetStatus($"Room {LauncherArgs.Room}: {response.readyPlayerCount}/{response.playerCount} ready; waiting for {response.unreadyPlayerCount} player(s) to return to the lobby.");
        else if (response.playerCount < 2)
            SetStatus($"Room {LauncherArgs.Room}: waiting for at least one more player. Maximum {response.maxPlayers}.");
        else
            SetStatus(response.isHost
                ? $"Room {LauncherArgs.Room}: all {response.playerCount} players are ready. You can start."
                : $"Room {LauncherArgs.Room}: all players are ready. Waiting for the host to start.");
        string roomNotice = ConsumeRoomEvent(response.gameEvent);
        if (!string.IsNullOrEmpty(roomNotice))
            SetStatus(roomNotice);
        SetRoomButtonsInteractable(!roomActionInProgress);
    }

    private string ConsumeRoomEvent(GameEventDto roomEvent)
    {
        if (roomEvent == null || roomEvent.eventId <= lastRoomEventId)
            return string.Empty;

        lastRoomEventId = roomEvent.eventId;
        switch (roomEvent.type)
        {
            case "PLAYER_JOINED":
                return $"{roomEvent.username} joined room {LauncherArgs.Room}.";
            case "PLAYER_LEFT":
                return $"{roomEvent.username} left room {LauncherArgs.Room}.";
            case "PLAYER_DISCONNECTED":
                return $"{roomEvent.username} disconnected from the room.";
            case "PLAYER_RECONNECTED":
                return $"{roomEvent.username} reconnected to the room.";
            case "PLAYER_RETURNED_TO_LOBBY":
                return $"{roomEvent.username} returned to the room lobby.";
            case "ROOM_RESET":
                return "All players are back. The room is ready for another match.";
            default:
                return string.Empty;
        }
    }

    private void SetRoomEntryVisible(bool visible)
    {
        if (roomInput != null)
            roomInput.gameObject.SetActive(visible);
        if (createButton != null)
            createButton.SetActive(visible);
        if (joinButton != null)
            joinButton.SetActive(visible);
    }

    private string GetMyPlayerId() =>
        string.IsNullOrWhiteSpace(LauncherArgs.RacerId) ? LauncherArgs.User : LauncherArgs.RacerId;

    private void CreateRoomActionUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        roomActionsRoot = new GameObject("RoomActions", typeof(RectTransform));
        roomActionsRoot.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform root = roomActionsRoot.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(0f, 340f);
        root.sizeDelta = new Vector2(960f, 116f);

        GameObject labelObject = new GameObject("RoomAndPlayerId", typeof(RectTransform));
        labelObject.transform.SetParent(roomActionsRoot.transform, false);
        RectTransform labelRect = labelObject.GetComponent<RectTransform>();
        labelRect.anchorMin = labelRect.anchorMax = new Vector2(0.5f, 0.5f);
        labelRect.anchoredPosition = new Vector2(0f, 34f);
        labelRect.sizeDelta = new Vector2(920f, 44f);
        roomCodeLabel = labelObject.AddComponent<TextMeshProUGUI>();
        roomCodeLabel.fontSize = 24f;
        roomCodeLabel.fontStyle = FontStyles.Bold;
        roomCodeLabel.alignment = TextAlignmentOptions.Center;
        roomCodeLabel.color = Color.white;
        roomCodeLabel.textWrappingMode = TextWrappingModes.NoWrap;
        roomCodeLabel.text = $"ROOM {LauncherArgs.Room}  |  ID {GetMyPlayerId()}";

        CreateRoomActionButton("COPY ROOM CODE", new Vector2(-120f, -28f), () =>
        {
            CopyMaPhong();
            SetStatus("Room code copied.");
        });
        CreateRoomActionButton("COPY MY ID", new Vector2(120f, -28f), () =>
        {
            GUIUtility.systemCopyBuffer = GetMyPlayerId();
            SetStatus("Your player ID copied.");
        });
        roomActionsRoot.SetActive(false);
    }

    private void CreateRoomActionButton(string label, Vector2 position, UnityEngine.Events.UnityAction onClick)
    {
        GameObject buttonObject = new GameObject(label.Replace(' ', '_'), typeof(RectTransform), typeof(Image), typeof(Button));
        buttonObject.transform.SetParent(roomActionsRoot.transform, false);
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        rect.anchorMin = rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = new Vector2(220f, 48f);

        Image image = buttonObject.GetComponent<Image>();
        image.color = new Color(0.12f, 0.42f, 0.32f, 0.96f);
        Button button = buttonObject.GetComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(onClick);

        GameObject textObject = new GameObject("Label", typeof(RectTransform));
        textObject.transform.SetParent(buttonObject.transform, false);
        RectTransform textRect = textObject.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = textRect.offsetMax = Vector2.zero;
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = label;
        text.fontSize = 18f;
        text.fontStyle = FontStyles.Bold;
        text.alignment = TextAlignmentOptions.Center;
        text.color = Color.white;
        text.raycastTarget = false;
    }

    private void CreateRoomRosterUi()
    {
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas == null)
            return;

        roomRosterRoot = new GameObject("RoomRoster", typeof(RectTransform), typeof(Image));
        roomRosterRoot.transform.SetParent(canvas.rootCanvas.transform, false);
        RectTransform root = roomRosterRoot.GetComponent<RectTransform>();
        root.anchorMin = root.anchorMax = new Vector2(0.5f, 0.5f);
        root.pivot = new Vector2(0.5f, 0.5f);
        root.anchoredPosition = new Vector2(-650f, -70f);
        root.sizeDelta = new Vector2(340f, 280f);
        roomRosterRoot.GetComponent<Image>().color = new Color(0.04f, 0.05f, 0.07f, 0.82f);

        TMP_Text title = CreateRosterText(root, "PlayersTitle", "ROOM PLAYERS", 20f, FontStyles.Bold);
        RectTransform titleRect = title.rectTransform;
        titleRect.anchorMin = titleRect.anchorMax = new Vector2(0f, 1f);
        titleRect.pivot = new Vector2(0f, 1f);
        titleRect.anchoredPosition = new Vector2(18f, -12f);
        titleRect.sizeDelta = new Vector2(300f, 30f);

        for (int i = 0; i < 4; i++)
        {
            GameObject avatarObject = new GameObject("PlayerAvatar" + i, typeof(RectTransform), typeof(Image));
            avatarObject.transform.SetParent(root.transform, false);
            RectTransform avatarRect = avatarObject.GetComponent<RectTransform>();
            avatarRect.anchorMin = avatarRect.anchorMax = new Vector2(0f, 1f);
            avatarRect.pivot = new Vector2(0f, 1f);
            avatarRect.anchoredPosition = new Vector2(18f, -50f - i * 52f);
            avatarRect.sizeDelta = new Vector2(40f, 40f);
            Image avatar = avatarObject.GetComponent<Image>();
            avatar.color = new Color(0.3f, 0.34f, 0.4f, 1f);
            roomPlayerAvatars.Add(avatar);

            TMP_Text label = CreateRosterText(root, "PlayerName" + i, "Waiting...", 17f, FontStyles.Normal);
            RectTransform labelRect = label.rectTransform;
            labelRect.anchorMin = labelRect.anchorMax = new Vector2(0f, 1f);
            labelRect.pivot = new Vector2(0f, 1f);
            labelRect.anchoredPosition = new Vector2(72f, -50f - i * 52f);
            labelRect.sizeDelta = new Vector2(250f, 40f);
            roomPlayerLabels.Add(label);
        }

        roomRosterRoot.SetActive(false);
    }

    private static TMP_Text CreateRosterText(Transform parent, string name, string value, float fontSize, FontStyles style)
    {
        GameObject textObject = new GameObject(name, typeof(RectTransform));
        textObject.transform.SetParent(parent, false);
        TMP_Text text = textObject.AddComponent<TextMeshProUGUI>();
        text.text = value;
        text.fontSize = fontSize;
        text.fontStyle = style;
        text.color = Color.white;
        text.alignment = TextAlignmentOptions.MidlineLeft;
        text.textWrappingMode = TextWrappingModes.NoWrap;
        text.overflowMode = TextOverflowModes.Ellipsis;
        text.raycastTarget = false;
        return text;
    }

    private void UpdateRoomRoster(List<RoomPlayerDto> players)
    {
        for (int i = 0; i < roomPlayerLabels.Count; i++)
        {
            bool hasPlayer = players != null && i < players.Count && players[i] != null;
            roomPlayerLabels[i].gameObject.SetActive(hasPlayer);
            roomPlayerAvatars[i].gameObject.SetActive(hasPlayer);
            if (!hasPlayer)
                continue;

            RoomPlayerDto player = players[i];
            string role = player.isHost ? "HOST" : "GUEST";
            string readiness = player.isReturnedToLobby
                ? "RETURNED TO LOBBY"
                : player.isReady ? "READY" : $"IN {player.sceneName}";
            roomPlayerLabels[i].text = $"{player.username}  {role}  ·  {readiness}";
            if (roomAvatarSprites.TryGetValue(player.userId, out Sprite sprite))
                roomPlayerAvatars[i].sprite = sprite;
            else if (requestedAvatarIds.Add(player.userId))
                _ = LoadRoomAvatarAsync(player.userId);
        }
    }

    private async System.Threading.Tasks.Task LoadRoomAvatarAsync(int userId)
    {
        pendingAvatarRequests.Add(userId);
        try
        {
            Resp response = await ServerClient.CallAsync(new Req { command = "LAYAVATAR", userId = userId });
            if (this == null || response == null || !response.success ||
                string.IsNullOrWhiteSpace(response.avatarBase64))
                return;

            byte[] bytes = Convert.FromBase64String(response.avatarBase64);
            Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            if (!texture.LoadImage(bytes))
            {
                Destroy(texture);
                return;
            }

            Sprite sprite = Sprite.Create(texture, new Rect(0f, 0f, texture.width, texture.height), new Vector2(0.5f, 0.5f));
            roomAvatarTextures[userId] = texture;
            roomAvatarSprites[userId] = sprite;
            Resp state = await ServerClient.CallAsync(new Req { command = "ROOM_STATE" });
            if (state != null && state.success)
                UpdateRoomRoster(state.roomPlayers);
        }
        catch (Exception ex)
        {
            Debug.LogWarning("[LobbyManager] Room avatar could not be loaded: " + ex.GetType().Name);
        }
        finally
        {
            pendingAvatarRequests.Remove(userId);
        }
    }

    private void LoadGame(Resp response)
    {
        if (loadingGame)
            return;
        loadingGame = true;
        StopRoomMonitor();

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
