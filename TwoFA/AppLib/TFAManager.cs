using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace TwoFA.AppLib
{
    /* ****************************************************************************
     * Key Uri Format (space: %20)
     * https://rootprojects.org/authenticator/
     * ----------------------------------------------------------------------------
     * Secret keys may be encoded in QR codes as a URI with the following format:
     * 
     * otpauth://TYPE/LABEL?PARAMETERS
     * 
     * Valid types are "hotp" and "totp", to distinguish whether the key will be used for counter-based HOTP or for TOTP.
     * 
     * The label is used to identify which account a key is associated with. Ex: "**":"**"
     * 
     * The issuer prefix and account name should be separated by a literal or url-encoded colon, and optional spaces may precede 
     * the account name. Neither issuer nor account name may themselves contain a colon.
     * 
     * Parameters
     * 
     * Secret - REQUIRED: The secret parameter is an arbitrary key value encoded in Base32
     * Issuer - STRONGLY RECOMMENDED: The issuer parameter is a string value (URL-encoded) indicating the provider or service this account is associated with
     * Algorithm - OPTIONAL: The algorithm may have the values: SHA1 (Default) SHA256 SHA512
     * Digits - OPTIONAL: The digits parameter may have the values 6 or 8, and determines how long of a one-time passcode to display to the user. The default is 6.
     * Counter - REQUIRED if type is hotp: The counter parameter is required when provisioning a key for use with HOTP. It will set the initial counter value.
     * Period - OPTIONAL only if type is totp: The period parameter defines a period that a TOTP code will be valid for, in seconds. The default value is 30.
     * 
     * 
     ****************************************************************************** */

    public class TFAManager
    {
        public const string AppUriFormat = "otpauth://totp/{0}:{1}?secret={2}&issuer={0}&digits=6";

        /// <summary>
        /// Generates a random 160-bit Base32-Encoded security key 
        /// </summary>
        /// <returns></returns>
        public static string GetAuthenticatorKey()
        {
            using (RandomNumberGenerator generator = RandomNumberGenerator.Create())
            {
                byte[] bytes = new byte[20];  // size of SHA1 hash          
                generator.GetBytes(bytes);
                return Base32.ToBase32(bytes);
            }
        }

        public static string FormatKey(string unformattedKey)
        {
            int currentPosition = 0;

            StringBuilder result = new StringBuilder();

            while (currentPosition + 4 < unformattedKey.Length)
            {
                result.Append(unformattedKey.Substring(currentPosition, 4)).Append(" ");
                currentPosition += 4;
            }

            if (currentPosition < unformattedKey.Length) result.Append(unformattedKey.Substring(currentPosition));

            return result.ToString().ToLowerInvariant();
        }

        public static string QrCodeUri(string issuer, string accountId, string unformattedKey)
        {
            return string.Format(AppUriFormat, System.Web.HttpUtility.UrlEncode(issuer), System.Web.HttpUtility.UrlEncode(accountId), unformattedKey);
        }

        public static int GetAuthenticatorCode(string unformattedKey)
        {
            long unixTimestamp = (DateTime.UtcNow.Ticks - 621355968000000000L) / 10000000L;

            long window = unixTimestamp / (long)30;

            byte[] keyBytes = Base32.FromBase32(unformattedKey);

            byte[] counter = BitConverter.GetBytes(window);

            if (BitConverter.IsLittleEndian) Array.Reverse(counter);

            HMACSHA1 hmac = new HMACSHA1(keyBytes);

            byte[] hash = hmac.ComputeHash(counter);

            int offset = hash[^1] & 0xf;

            // Convert the 4 bytes into an integer, ignoring the sign.
            var binary = ((hash[offset] & 0x7f) << 24) | (hash[offset + 1] << 16) | (hash[offset + 2] << 8) | (hash[offset + 3]);

            return binary % (int)Math.Pow(10, 6);
        }

        public static long GetCurrentCounter()
        {
            DateTime unixEpoch = new DateTime(1970, 1, 1, 0, 0, 0, DateTimeKind.Utc);

            return 30 - (long)(DateTime.UtcNow - unixEpoch).TotalSeconds % 30;
        }
    }
}
