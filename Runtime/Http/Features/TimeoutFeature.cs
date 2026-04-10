using System;
using R3;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 요청에 타임아웃을 적용하는 기능.
    /// 지정 시간 내에 응답이 없으면 HttpErrorKind.Timeout 에러를 발생시킵니다.
    ///
    /// 사용 예:
    ///   var api = new ApiModel&lt;Unit, DataDto&gt;(client, "/api/data", HttpMethod.GET)
    ///       .With(new TimeoutFeature(seconds: 30f));
    /// </summary>
    public class TimeoutFeature : ViewModelFeature
    {
        public PropModel<float> Duration { get; }

        /// <param name="seconds">타임아웃 시간(초)</param>
        public TimeoutFeature(float seconds = 30f)
        {
            Duration = new PropModel<float>(seconds);
            Duration.AddTo(this);
        }

        /// <summary>
        /// Observable에 타임아웃을 적용합니다.
        /// 타임아웃 초과 시 TimeoutException을 HttpError(Timeout)로 변환합니다.
        /// </summary>
        public Observable<T> Apply<T>(Observable<T> source)
        {
            var timeout = TimeSpan.FromSeconds(Duration.Value);
            return source
                .Timeout(timeout)
                .Catch<T, TimeoutException>(_ =>
                    Observable.Throw<T>(new HttpError(0, "Request timed out", HttpErrorKind.Timeout)));
        }

        public override void Dispose()
        {
            base.Dispose();
            Duration.Dispose();
        }
    }
}
