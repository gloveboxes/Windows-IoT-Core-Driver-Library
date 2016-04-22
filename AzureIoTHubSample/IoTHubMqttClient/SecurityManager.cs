using System;
using System.Collections.Generic;
using System.Globalization;
using System.Net;
using System.Runtime.InteropServices.WindowsRuntime;
using System.Text;
using Windows.Security.Cryptography;
using Windows.Security.Cryptography.Core;

namespace IoTHubMqttClient {
    public sealed class SecurityManager {

        public string hubAddress { get; private set; }
        public string hubName { get; private set; }
        string SharedAccessKey; 

        public string hubPass => GenerateSas(); 

        const char Base64Padding = '=';
        readonly HashSet<char> base64Table = new HashSet<char>{'A','B','C','D','E','F','G','H','I','J','K','L','M','N','O',
                                                                      'P','Q','R','S','T','U','V','W','X','Y','Z','a','b','c','d',
                                                                      'e','f','g','h','i','j','k','l','m','n','o','p','q','r','s',
                                                                      't','u','v','w','x','y','z','0','1','2','3','4','5','6','7',
                                                                      '8','9','+','/' };


        public SecurityManager(string connectionString) {
            loadConfig(connectionString);
        }

        private void loadConfig(string connectionString) {
            string[] pairs = connectionString.Split(';');

            foreach (var pair in pairs) {
                string[] keyValues = pair.Split('=');
                if (keyValues.Length > 1) {
                    switch (keyValues[0].ToLower()) {
                        case "hostname":
                            hubAddress = keyValues[1];
                            break;
                        case "deviceid":
                            hubName = keyValues[1];
                            break;
                        case "sharedaccesskey":
                            SharedAccessKey = pair.Substring(16);  // special processing for SharedAccessKey because we can't just split it by '='. The key itself may contain numerous '='
                            break;
                    }
                }
            }
        }

        public string GenerateSas() {
            try {
                string sr = $"{hubAddress}/devices/{hubName}";
                string sas = BuildSignature(null, SharedAccessKey, sr, TimeSpan.FromDays(50));
                return $"SharedAccessSignature {sas}";
            }
            catch (Exception) {
                return null;
            }
        }

        string BuildSignature(string keyName, string key, string target, TimeSpan timeToLive) {
            string expiresOn = BuildExpiresOn(timeToLive);
            string audience = WebUtility.UrlEncode(target);
            List<string> fields = new List<string>();
            fields.Add(audience);
            fields.Add(expiresOn);

            // Example string to be signed:
            // dh://myiothub.azure-devices.net/a/b/c?myvalue1=a
            // <Value for ExpiresOn>

            string signature = Sign(string.Join("\n", fields), key);

            // Example returned string:
            // SharedAccessSignature sr=ENCODED(dh://myiothub.azure-devices.net/a/b/c?myvalue1=a)&sig=<Signature>&se=<ExpiresOnValue>[&skn=<KeyName>]

            var buffer = new StringBuilder();
            buffer.AppendFormat(CultureInfo.InvariantCulture, "sr={0}&sig={1}&se={2}",
                                audience,
                                WebUtility.UrlEncode(signature),
                                WebUtility.UrlEncode(expiresOn));

            if (!string.IsNullOrEmpty(keyName)) {
                buffer.AppendFormat(CultureInfo.InvariantCulture, "&{0}={1}",
                    "skn", WebUtility.UrlEncode(keyName));
            }

            return buffer.ToString();
        }

        string BuildExpiresOn(TimeSpan timeToLive) {
            DateTime expiresOn = DateTime.UtcNow; //Util.CorrectedUtcTime.Add(timeToLive);
            TimeSpan secondsFromBaseTime = expiresOn.Subtract(new DateTime(1970, 1, 1, 0, 0, 0, 0, DateTimeKind.Utc));
            long seconds = Convert.ToInt64(secondsFromBaseTime.TotalSeconds, CultureInfo.InvariantCulture);
            return Convert.ToString(seconds, CultureInfo.InvariantCulture);
        }

        string Sign(string requestString, string key) {
            if (!IsBase64String(key))
                throw new ArgumentException("The SharedAccessKey of the device is not a Base64String");

            var algo = MacAlgorithmProvider.OpenAlgorithm(MacAlgorithmNames.HmacSha256);
            var keyMaterial = Convert.FromBase64String(key).AsBuffer();
            var hash = algo.CreateHash(keyMaterial);
            hash.Append(CryptographicBuffer.ConvertStringToBinary(requestString, BinaryStringEncoding.Utf8));

            var sign = CryptographicBuffer.EncodeToBase64String(hash.GetValueAndReset());
            return sign;
        }

        public bool IsBase64String(string data) {
            data = data.Replace("\r", string.Empty).Replace("\n", string.Empty);

            if (data.Length == 0 || (data.Length % 4) != 0) {
                return false;
            }

            var lengthNoPadding = data.Length;
            data = data.TrimEnd(Base64Padding);
            var lengthPadding = data.Length;

            if ((lengthNoPadding - lengthPadding) > 2) {
                return false;
            }

            foreach (char c in data) {
                if (!base64Table.Contains(c)) {
                    return false;
                }
            }
            return true;
        }
    }
}
