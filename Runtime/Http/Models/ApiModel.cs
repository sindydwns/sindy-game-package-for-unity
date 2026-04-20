using System;
using System.Collections.Generic;
using Newtonsoft.Json;
using R3;
using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// REST 엔드포인트 하나를 대표하는 ViewModel.
    ///
    /// Request(SubjModel) 로 요청을 발행하고, Response(ApiResponseModel) 로 결과를 구독합니다.
    /// Feature 패턴(.With())으로 RetryFeature / TimeoutFeature / AuthFeature를 조합할 수 있습니다.
    ///
    /// 사용 예:
    ///   var api = new ApiModel<LoginReq, LoginRes>(client, "/api/login");
    ///   api.Request.Send(new LoginReq { ... });
    ///   api.Response.Data.Subscribe(res => ...).AddTo(disposables);
    /// </summary>
    public class ApiModel<TReq, TRes> : ViewModel
    {
        public ApiRequestModel<TReq> Request { get; }
        public ApiResponseModel<TRes> Response { get; }

        public new ApiModel<TReq, TRes> With<T>(T feature) where T : ViewModelFeature
        {
            base.With(feature);
            return this;
        }

        public ApiModel(IHttpClient client, string url, HttpMethod method = HttpMethod.POST)
        {
            Request = new ApiRequestModel<TReq>();
            Response = new ApiResponseModel<TRes>();

            this["request"] = Request;
            this["response"] = Response;

            // 요청 발행 → 로딩 설정 → HTTP 전송 (+ Feature 적용) → 응답 갱신
            // SelectMany 내부에서 에러를 잡아 Response.SetError()로 전달하고
            // Empty를 반환해 Subject 스트림이 끊기지 않도록 합니다.
            Request.Obs
                .Do(_ => Response.SetLoading())
                .SelectMany(body =>
                    ApplyFeatures(() => Send(client, url, method, body))
                        .Catch<HttpResponse<TRes>, Exception>(err =>
                        {
                            var httpErr = err as HttpError
                                ?? new HttpError(0, err.Message, HttpErrorKind.Network, err);
                            Response.SetError(httpErr);
                            return Observable.Empty<HttpResponse<TRes>>();
                        })
                )
                .Subscribe(res => Response.SetSuccess(res))
                .AddTo(disposables);
        }

        /// <summary>
        /// 등록된 Feature들을 파이프라인에 적용합니다.
        /// RetryFeature: 팩토리를 재호출하여 재시도
        /// TimeoutFeature: 응답 대기 제한 시간 적용
        /// </summary>
        private Observable<HttpResponse<TRes>> ApplyFeatures(
            Func<Observable<HttpResponse<TRes>>> factory)
        {
            var retry = Feature<RetryFeature>();
            var timeout = Feature<TimeoutFeature>();

            if (retry != null)
            {
                // RetryFeature가 팩토리를 감싸서 재시도 시 새 요청 생성
                if (timeout != null)
                    return retry.Apply(() => timeout.Apply(factory()));

                return retry.Apply(factory);
            }

            if (timeout != null)
                return timeout.Apply(factory());

            return factory();
        }

        private static Observable<HttpResponse<TRes>> Send(
            IHttpClient client, string url, HttpMethod method, TReq body)
        {
            var req = new HttpRequest
            {
                Url = url,
                Method = method,
                Body = SerializeBody(method, body),
                Headers = new Dictionary<string, string>
                {
                    ["Accept"] = "application/json",
                },
            };
            return client.Send<TRes>(req);
        }

        private static string SerializeBody(HttpMethod method, TReq body)
        {
            if (method is HttpMethod.GET or HttpMethod.DELETE) return null;
            if (body is Unit) return null;
            if (body == null) return "{}";
            return JsonConvert.SerializeObject(body);
        }

        public override void Dispose()
        {
            base.Dispose();
            Request.Dispose();
            Response.Dispose();
        }
    }
}
