using System.Runtime.InteropServices;

namespace Listik
{
    internal enum LicenseValidationResult
    {
        Active,
        Inactive,
        NetworkError
    }

    internal sealed class LicenseCheckInfo
    {
        public LicenseValidationResult Result { get; set; }
        public string Message { get; set; }
        public int RemainingDays { get; set; }
    }

    // Network communication and the subscription state are implemented in Hook.dll.
    // This class only transfers the activation data to the DLL and reads its result.
    internal static class LicenseService
    {
        [DllImport("Hook.dll", CallingConvention = CallingConvention.Cdecl,
            CharSet = CharSet.Unicode, ExactSpelling = true)]
        private static extern int StartSubscriptionMonitoring(string code, string deviceId,
            out int remainingDays);

        [DllImport("Hook.dll", CallingConvention = CallingConvention.Cdecl,
            ExactSpelling = true)]
        private static extern int GetSubscriptionStatus(out int remainingDays);

        public static LicenseValidationResult Activate(string code, string deviceId,
            out string message, out int remainingDays)
        {
            remainingDays = 0;
            var status = StartSubscriptionMonitoring(code, deviceId, out remainingDays);
            switch (status)
            {
                case 1:
                    message = "Подписка активирована.";
                    return LicenseValidationResult.Active;
                case 0:
                    message = "Подписка не активна или код доступа неверный.";
                    return LicenseValidationResult.Inactive;
                default:
                    message = "Не удалось проверить подписку. Проверьте интернет и повторите попытку.";
                    return LicenseValidationResult.NetworkError;
            }
        }

        public static LicenseCheckInfo Check(string code, string deviceId)
        {
            int remainingDays;
            var status = GetSubscriptionStatus(out remainingDays);
            var result = ToResult(status, out var message);
            return new LicenseCheckInfo
            {
                Result = result,
                Message = message,
                RemainingDays = remainingDays
            };
        }

        private static LicenseValidationResult ToResult(int status, out string message)
        {
            switch (status)
            {
                case 1:
                    message = "Подписка активирована.";
                    return LicenseValidationResult.Active;
                case 0:
                    message = "Подписка не активна или код доступа неверный.";
                    return LicenseValidationResult.Inactive;
                default:
                    message = "Не удалось проверить подписку. Проверьте интернет и повторите попытку.";
                    return LicenseValidationResult.NetworkError;
            }
        }
    }
}
