namespace DanToPhong.Protocol
{
    public static class Commands
    {
        public const string Register = "DANGKY";
        public const string Login = "DANGNHAP";
        public const string GetInfo = "LAYTHONGTIN";
        public const string Logout = "DANGXUAT";
        public const string ResetPassword = "DOIMATKHAU";
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