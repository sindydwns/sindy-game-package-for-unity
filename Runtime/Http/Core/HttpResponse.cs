namespace Sindy.Http
{
    /// <summary>
    /// HTTP 응답 데이터.
    /// IHttpClient.Send()의 Observable이 OnNext로 전달합니다.
    /// </summary>
    public struct HttpResponse<T>
    {
        public int    StatusCode { get; set; }
        public T      Data       { get; set; }
        public string RawJson    { get; set; }

        public bool IsSuccess => StatusCode is >= 200 and < 300;
    }
}
