using System.Collections.Generic;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 요청 데이터.
    /// ApiModel 내부에서 생성되며, IHttpClient.Send()에 전달됩니다.
    /// </summary>
    public struct HttpRequest
    {
        public string Url { get; set; }
        public HttpMethod Method { get; set; }

        /// <summary>직렬화된 JSON 문자열. GET/DELETE 시 null.</summary>
        public string Body { get; set; }
        public Dictionary<string, string> Headers { get; set; }

        public override string ToString() => $"{Method} {Url}";
    }
}
