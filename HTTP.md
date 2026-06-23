# HTTP 모듈

> R3 + UnityWebRequest 기반 HTTP 통신 모듈. `ApiModel`(요청/응답 ViewModel)과
> Feature 합성(`RetryFeature`/`TimeoutFeature`/`OfflineCacheFeature`)으로
> View/MVVM 시스템([SINDY_COMPONENT.md](SINDY_COMPONENT.md))과 동일한 패턴으로 서버 통신을 다룹니다.
> 네임스페이스: `Sindy.Http`. 직렬화는 Newtonsoft.Json.

## 폴더 구조

```
Runtime/Http/
├── Core/       IHttpClient, UnityWebRequestClient, HttpRequest/Response/Error
├── Models/     ApiModel, ApiRequestModel, ApiResponseModel, PaginatedApiModel, PagedResponse
├── Features/   RetryFeature, TimeoutFeature, OfflineCacheFeature
└── Auth/       TokenModel, ITokenStore, AuthenticatedHttpClient, TokenRefreshService,
                AuthService, IOAuthProvider
```

테스트: `Tests/Runtime/HttpTest/` (Phase 1~5 시나리오), `Tests/Editor/HttpTests.cs` (NUnit).

---

## Core — 저수준 클라이언트

```csharp
public interface IHttpClient
{
    Observable<HttpResponse<T>> Send<T>(HttpRequest request, CancellationToken ct = default);
    Observable<HttpResponse<T>> Get<T>(string url, Dictionary<string, string> headers = null, CancellationToken ct = default);
    Observable<HttpResponse<T>> Post<T>(string url, string jsonBody, Dictionary<string, string> headers = null, CancellationToken ct = default);
    Observable<HttpResponse<T>> Put<T>(string url, string jsonBody, Dictionary<string, string> headers = null, CancellationToken ct = default);
    Observable<HttpResponse<T>> Delete<T>(string url, Dictionary<string, string> headers = null, CancellationToken ct = default);
}
```

- 성공: `OnNext(HttpResponse<T>)` → `OnCompleted` / 실패: `OnError(HttpError)`
- 기본 구현은 `UnityWebRequestClient(baseUrl = "")` — 상대 경로 요청 시 baseUrl이 접두됩니다.

```csharp
public struct HttpResponse<T>
{
    public int    StatusCode;
    public T      Data;          // JSON 역직렬화 결과
    public string RawJson;
    public bool   IsSuccess;     // 200 <= StatusCode < 300
}

public class HttpError : Exception
{
    public int           StatusCode { get; }
    public string        RawBody    { get; }
    public HttpErrorKind Kind       { get; }   // Network/Timeout/Unauthorized/ServerError 등
}
```

`HttpError.Kind`로 분기하면 상태 코드 매핑(`KindFromStatusCode`)을 직접 작성할 필요가 없습니다.

직접 사용 예 (보통은 아래 `ApiModel`을 권장):

```csharp
var client = new UnityWebRequestClient("https://api.example.com");
client.Get<UserDto>("/users/me")
    .Subscribe(
        res => Debug.Log(res.Data.Name),
        err => Debug.LogError(((HttpError)err).Kind))
    .AddTo(disposables);
```

---

## ApiModel — 엔드포인트 하나 = ViewModel 하나

`ApiModel<TReq, TRes>`는 REST 엔드포인트 하나를 대표하는 ViewModel입니다.
`Request`(SubjModel)로 요청을 발행하고, `Response`(상태 묶음)를 UI에 그대로 바인딩합니다.

```csharp
public class ApiModel<TReq, TRes> : ViewModel
{
    public ApiRequestModel<TReq>  Request  { get; }   // Send(body)로 요청 발행
    public ApiResponseModel<TRes> Response { get; }   // Data/IsLoading/Error/HasError
    public ApiModel(IHttpClient client, string url, HttpMethod method = HttpMethod.POST);
}

public class ApiResponseModel<T> : ViewModel
{
    public PropModel<T>         Data      { get; }
    public PropModel<bool>      IsLoading { get; }
    public PropModel<HttpError> Error     { get; }
    public PropModel<bool>      HasError  { get; }
}
```

사용:

```csharp
var loginApi = new ApiModel<LoginReq, LoginRes>(client, "/api/login");

loginApi.Response.Data.Subscribe(res => OnLogin(res)).AddTo(disposables);
loginApi.Response.IsLoading.Subscribe(on => spinnerModel.Feature<VisibilityFeature>().Show.Value = on).AddTo(disposables);
loginApi.Response.Error.Subscribe(err => { if (err != null) ShowError(err.Kind); }).AddTo(disposables);

loginApi.Request.Send(new LoginReq { Id = "sindy" });   // 발행 → 로딩 → 응답/에러 자동 갱신
```

동작 규칙:

- `GET`/`DELETE`는 body를 직렬화하지 않습니다. body가 필요 없는 요청은 `TReq = Unit`을 사용합니다.
- 에러가 나도 내부 스트림은 끊기지 않습니다 — `Response.Error`에 전달 후 다음 `Send`를 계속 받습니다.
- `Request`/`Response`는 `this["request"]`/`this["response"]` 자식으로도 등록되어
  SindyComponent 키 매핑에 그대로 올릴 수 있습니다.
- `Dispose()`가 Request/Response를 함께 정리합니다.

---

## Feature 합성 — Retry / Timeout / OfflineCache

View 시스템과 동일하게 `.With()`로 능력을 조합합니다.

```csharp
var api = new ApiModel<Unit, RankingRes>(client, "/api/ranking", HttpMethod.GET)
    .With(new RetryFeature(maxRetry: 3, baseDelay: 1f))
    .With(new TimeoutFeature(seconds: 10f));
```

| Feature | 생성자 | 상태 | 동작 |
|---|---|---|---|
| `RetryFeature` | `(maxRetry = 3, baseDelay = 1f, retryOnServerError = false)` | `IsRetrying` | 지수 백오프 재시도. 요청 팩토리를 재호출해 매번 새 요청 생성 |
| `TimeoutFeature` | `(seconds = 30f)` | `Duration` | 응답 대기 제한. 초과 시 `HttpErrorKind.Timeout` |
| `OfflineCacheFeature<T>` | `(TimeSpan maxAge)` | `IsFromCache`, `CachedAt` | 성공 응답 캐싱, 네트워크 실패 시 유효 기간 내 캐시로 폴백 |

적용 순서는 `ApiModel`이 내부에서 처리합니다: Retry가 바깥(재시도마다 Timeout 새로 적용).
`RetryFeature.IsRetrying`을 구독해 "재시도 중..." UI를 띄울 수 있습니다.

---

## Auth — 토큰 관리와 자동 갱신

```
UnityWebRequestClient (네트워크)
        ▲
AuthenticatedHttpClient (Authorization 헤더 주입 + 401 시 토큰 갱신 후 1회 재시도)
        ▲
ApiModel (UI 바인딩)
```

```csharp
// 1) 전역 1회 초기화
var tokenStore   = new PlayerPrefsTokenStore();          // ITokenStore 구현
var tokenModel   = new TokenModel();                     // AccessToken/RefreshToken/IsExpired
var rawClient    = new UnityWebRequestClient(baseUrl);
var refreshSvc   = new TokenRefreshService(rawClient, "/auth/refresh");
var client       = new AuthenticatedHttpClient(rawClient, tokenModel, refreshSvc);

// 2) 이후 모든 ApiModel은 client만 받으면 인증이 자동 처리됨
var profileApi = new ApiModel<Unit, ProfileRes>(client, "/api/me", HttpMethod.GET);
```

- `TokenModel.Update(access, refresh, expiresInSeconds)` — 로그인 성공 시 호출. `HasToken`, `IsExpired` 제공
- `AuthenticatedHttpClient`는 `HttpErrorKind.Unauthorized`(401)일 때만,
  토큰이 있을 때만 `TokenRefreshService.Refresh()` 후 **딱 1회** 재시도합니다.
- `ITokenStore`(`Save`/`Load`/`Clear`)로 저장소 교체 가능 — 보안이 중요하면
  `PlayerPrefsTokenStore` 대신 플랫폼 보안 저장소 구현을 권장합니다.

### 소셜 로그인 (OAuth2)

플랫폼별 SDK 연동은 `IOAuthProvider` 구현으로 주입합니다 (패키지는 인터페이스만 제공):

```csharp
public interface IOAuthProvider
{
    string ProviderName { get; }
    Observable<TokenResponseDto> Login();
    Observable<Unit> Logout();
}
```

`AuthService(tokenModel)`가 로그인 상태를 ViewModel로 노출합니다:
`IsLoggedIn` / `IsLoading` / `ErrorMessage` / `OnLoginSuccess`, `LoginWith(provider)` / `Logout()`.
이 PropModel들을 FeatureView에 바인딩하면 로그인 화면이 코드 몇 줄로 끝납니다.

```csharp
var auth = new AuthService(tokenModel);
loginButton.Feature<ButtonFeature>().OnClick
    .Subscribe(_ => auth.LoginWith(googleProvider).Subscribe().AddTo(disposables))
    .AddTo(disposables);
auth.IsLoggedIn.Subscribe(on => { if (on) EnterMain(); }).AddTo(disposables);
```

---

## 페이지네이션 — PaginatedApiModel

서버 응답이 `PagedResponse<T>`(`Items`/`Page`/`TotalPages`) 형태일 때 페이지 UI를 자동화합니다.

```csharp
var ranking = new PaginatedApiModel<RankItemDto>(
    client,
    "/api/ranking",                       // ?page=N 자동 추가
    dto => new ViewModel()                // DTO → 셀 ViewModel 매핑
        .With(new TextFeature(dto.Name))
        .With(new TextFeature(dto.Score.ToString())));

ranking.GoToPage(1);
```

- `Items`(`PropModel<List<IViewModel>>`)를 `ListFeature`/`ScrollerFeature`에 연결
- `PrevButton`/`NextButton`은 `ButtonFeature + InteractableFeature` 조합 ViewModel —
  첫/마지막 페이지에서 자동 비활성화되고 클릭 시 `GoToPage`가 호출됩니다
- `CurrentPage`/`TotalPages`/`IsLoading`/`Error` 모두 PropModel이라 그대로 바인딩 가능

---

## 정리(cleanup)

`ApiModel`/`TokenModel`/`AuthService`는 모두 ViewModel이므로 정리 규칙도 동일합니다:

```csharp
hub.Bind(null);     // 1. FeatureView 구독 해제 (UI에 바인딩했다면)
apiModel.Dispose(); // 2. 모델 내부 스트림 해제
```

진행 중 요청을 끊으려면 `Send`/`Get` 계열에 `CancellationToken`을 전달하세요.
