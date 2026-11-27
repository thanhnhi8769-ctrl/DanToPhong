namespace DanToPhong.Protocol
{
    public static class Commands
    {
        public const string Register = "DANGKY";
        public const string Login = "DANGNHAP";
        public const string GetInfo = "LAYTHONGTIN";
        public const string Logout = "DANGXUAT";
        public const string ResetPassword = "DOIMATKHAU";
        public const string RequestResetCode = "YEUCAUMADATLAI";
        public const string RequestRegistrationCode = "YEUCAUMADANGKY";
        public const string UpdateCoins = "CAPNHATXU";
        public const string JoinRoom = "VAOPHONG";
        public const string ChangeAvatar = "DOIAVATAR";

        // MỚI
        public const string AuthToken = "XACTHUCTOKEN";
        public const string GetInventory = "LAYKHO";
        public const string BuyItem = "MUAHANG";
        public const string Equip = "TRANGBI";
        public const string SetMap = "CHONMAP";
        public const string UploadAvatar = "TAIAVATAR";
        public const string GetAvatar = "LAYAVATAR";
        public const string SubmitReview = "GUIDANHGIA";
        public const string GetReviews = "LAYDANHGIA";
        public const string Leaderboard = "BANGXEPHANG";
        public const string UpsertRoom = "CAPNHATPHONG";
        public const string RemoveRoom = "XOAPHONG";
        public const string ListRooms = "DSPHONG";
        public const string Invite = "MOIBAN";
        public const string GetInvites = "LAYLOIMOI";
        public const string GetState = "LAYTRANGTHAI";   // kho + lời mời trong một response
        public const string Ping = "PING";
        public const string CreateGameRoom = "ROOM_CREATE";
        public const string JoinGameRoom = "ROOM_JOIN";
        public const string LeaveGameRoom = "ROOM_LEAVE";
        public const string GameRoomState = "ROOM_STATE";
        public const string StartGameRoom = "ROOM_START";
        public const string GameInput = "GAME_INPUT";
        public const string GameStream = "GAME_STREAM";
        public const string GameSnapshot = "GAME_SNAPSHOT";
        public const string GameSpectate = "GAME_SPECTATE";
        public const string GameReconnect = "GAME_RECONNECT";
        public const string ChatSend = "CHAT_SEND";
        public const string ChatPoll = "CHAT_POLL";
        public const string GameEvent = "GAME_EVENT";
    }

    public static class ErrorCodes
    {
        public const string Ok = "OK";
        public const string InvalidInput = "ERR_INVALID_INPUT";
        public const string UserExists = "ERR_USER_EXISTS";
        public const string AuthFailed = "ERR_AUTH_FAILED";
        public const string Unauthorized = "ERR_UNAUTHORIZED";
        public const string InsufficientCoins = "ERR_INSUFFICIENT_COINS";
        public const string ServerError = "ERR_SERVER_ERROR";
        public const string AlreadyReviewed = "ERR_ALREADY_REVIEWED";
    }
}
