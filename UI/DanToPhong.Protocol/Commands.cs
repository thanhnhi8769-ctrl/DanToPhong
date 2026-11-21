namespace DanToPhong.Protocol
{
    public static class Commands
    {
        public const string Register = "DANGKY";
        public const string Login = "DANGNHAP";
        public const string GetInfo = "LAYTHONGTIN";
        public const string Logout = "DANGXUAT";
        public const string ResetPassword = "DOIMATKHAU";
    }

    public static class ErrorCodes
    {
        public const string Ok = "OK";
        public const string InvalidInput = "ERR_INVALID_INPUT";
        public const string UserExists = "ERR_USER_EXISTS";
        public const string AuthFailed = "ERR_AUTH_FAILED";
        public const string Unauthorized = "ERR_UNAUTHORIZED";
        public const string ServerError = "ERR_SERVER_ERROR";
    }
}