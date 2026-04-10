using Newtonsoft.Json;
using UnityEngine;

namespace Sindy.Http
{
    /// <summary>
    /// PlayerPrefs 기반 토큰 저장소.
    /// 간단한 프로토타입 용도로 사용합니다. 프로덕션에서는 SecureStorage를 권장합니다.
    /// </summary>
    public class PlayerPrefsTokenStore : ITokenStore
    {
        private const string Key = "sindy_auth_token";

        public void Save(string accessToken, string refreshToken)
        {
            var data = new TokenData
            {
                AccessToken  = accessToken,
                RefreshToken = refreshToken,
            };
            PlayerPrefs.SetString(Key, JsonConvert.SerializeObject(data));
            PlayerPrefs.Save();
        }

        public TokenData Load()
        {
            var json = PlayerPrefs.GetString(Key, null);
            if (string.IsNullOrEmpty(json)) return null;

            return JsonConvert.DeserializeObject<TokenData>(json);
        }

        public void Clear()
        {
            PlayerPrefs.DeleteKey(Key);
            PlayerPrefs.Save();
        }
    }
}
