// Mọi chữ hiển thị trong lobby/invite nằm ở đây. Đổi ngôn ngữ chỉ cần sửa file này.
public static class Txt
{
    // ---- Tên nút: PHẢI giống hệt chữ trên nút thật trong scene ----
    public const string BtnSelect = "SELECT VEHICLE & MAP";
    public const string BtnStart = "START GAME";
    public const string BtnConfirm = "CONFIRM";
    public const string BtnCopy = "COPY CODE";
    public const string BtnCopied = "COPIED!";

    // ---- Kết nối ----
    public const string NetworkError = "Unable to connect to Server. Check your Internet connection.\nPress ESC to return to the menu.";
    public const string Connecting = "Connecting to Server...";
    public const string Connected = "Connected! Click CREATE ROOM to get a code, or enter a code and click JOIN ROOM.";
    public const string NotReadyYet = "Not finished connecting to the Server, please wait 1 second...";
    public const string ServerNoResponse = "Server not responding. Please try again or press ESC to go back.";
    public static string Disconnected(object cause, int n, int max) => $"Disconnected ({cause}). Retrying {n}/{max}...";

    // ---- Phòng ----
    public const string LeavingRoom = "Leaving room...";
    public const string LeftRoom = "Left the room. Click CREATE ROOM or enter a code to JOIN ROOM.";
    public const string NeedRoomFirst = "Please CREATE ROOM or JOIN ROOM before clicking " + BtnSelect + "!";
    public const string EnterCode = "Please enter the ROOM CODE before clicking JOIN ROOM!";
    public const string CreateSendFailed = "Failed to send create room request. Please try again!";
    public const string JoinSendFailed = "Failed to send room request. Please try again!";
    public const string RoomNotFound = "Room not found. Please check the ROOM CODE!";
    public const string RoomFull = "Room is full (maximum 4 players)!";
    public const string RoomClosed = "The match in this room has already started!";
    public const string JoinFailed = "Unable to join the room. Please try again!";
    public static string WaitReload(int s) => $"Please wait {s}s for the game to reload...";
    public static string Creating(string code) => $"Creating room with code {code}...";
    public static string Searching(string code) => $"Searching for room code: {code}...";
    public static string CreateError(string msg) => $"Room creation error ({msg}). Please try creating again!";

    // ---- Người chơi ----
    public const string PlayerDefault = "Player";
    public const string TagHost = " (Host)";
    public const string TagYou = " (You)";
    public const string TagPicking = " - picking";
    public static string PlayerNum(int n) => $"Player #{n}";
    public static string PlayerJoined(string n) => $"{n} has joined the room!";
    public static string PlayerLeft(string n) => $"{n} has left the room!";
    public static string PlayerLeftCancel(string n) => $"{n} left. Not enough players (at least 2 required), countdown canceled!";
    public const string NewHostYou = "You are the new Room Host!";
    public static string NewHost(string n) => $"{n} is the new Room Host!";
    public const string NotEnoughPlayers = "Not enough players (at least 2 required). Countdown canceled!";

    // ---- Trạng thái phòng (3 dòng) ----
    public static string RoomLine(string code, int n, int max) => $"ROOM CODE: <color=#00FFFF><b>{code}</b></color>  ({n}/{max})";
    public static string WaitingConfirm(string names) => $"Waiting for {names} to click {BtnConfirm}...";
    public const string HintHost = "Click " + BtnStart + " to begin";
    public const string HintGuest = "Waiting for the Host to click " + BtnStart + "...";

    // ---- Start / nạp map ----
    public static string CannotStart(string names) => $"Cannot start yet: {names} is still in {BtnSelect} (has not clicked {BtnConfirm})...";
    public const string LoadingTrack = "Loading track...";
    public static string SceneMissing(string s) => $"Scene '{s}' not found. Add it to File > Build Settings!";
    public static string TrackMissing(string m) => $"Cannot load track '{m}'. Add it to File > Build Settings!";
    public const string TrackFailed = "<color=#FF6B6B>Track failed to load.</color>\nPress ESC to leave the room and try again.";

    // ---- Invite ----
    public const string InviteTitle = "INVITE BY PLAYER ID";
    public const string InvitePlaceholder = "ID or username";
    public const string BtnInvite = "INVITE";
    public const string Sending = "Sending...";
    public const string NoServer = "Cannot reach server.";
    public const string BtnJoin = "JOIN";
    public const string BtnDismiss = "DISMISS";
    public static string InvitedYou(string from, string code) => $"{from} invited you to room {code}";
}