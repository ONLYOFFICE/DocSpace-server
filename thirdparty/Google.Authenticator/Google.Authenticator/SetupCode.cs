namespace Google.Authenticator
{
    /// <summary>
    /// The setup TFA code parameters.
    /// </summary>
    public class SetupCode
    {
        /// <summary>
        /// The account for which the setup code is generated.
        /// </summary>
        public string Account { get; internal set; }
        
        /// <summary>
        /// The manual entry key.
        /// </summary>
        public string ManualEntryKey { get; internal set; }
        
        /// <summary>
        /// The QR-code setup image URL (base64-encoded PNG image).
        /// </summary>
        public string QrCodeSetupImageUrl { get; internal set; }

        public SetupCode() { }

        public SetupCode(string account, string manualEntryKey, string qrCodeSetupImageUrl)
        {
            Account = account;
            ManualEntryKey = manualEntryKey;
            QrCodeSetupImageUrl = qrCodeSetupImageUrl;
        }
    }
}