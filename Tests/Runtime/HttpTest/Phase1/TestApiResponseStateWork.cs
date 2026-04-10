using R3;
using Sindy.Http;
using UnityEngine.Assertions;

namespace Sindy.Test
{
    /// <summary>
    /// Phase 1 — ApiResponseModel 상태 전환 검증
    ///
    /// 검증 항목:
    ///   Case1: 초기 상태 확인 (IsLoading=false, HasError=false, Data=null)
    ///   Case2: 요청 중 IsLoading=true → 완료 후 false (지연 응답으로 중간 상태 검증)
    ///   Case3: 성공 → Error/HasError가 초기화되는지
    ///   Case4: 에러 → 이후 성공 요청 시 Error가 초기화되는지
    ///   Case5: Response.Data/IsLoading/Error PropModel 구독이 올바르게 동작하는지
    /// </summary>
    class TestApiResponseStateWork : TestCase
    {
        private class ItemDto { public string Id { get; set; } }

        public override void Run()
        {
            Case1_InitialState();
            Case2_LoadingTransition();
            Case3_ErrorClearedOnSuccess();
            Case4_DataPreservedOnError();
            Case5_PropModelSubscription();
        }

        // ── Case 1: 초기 상태 ────────────────────────────────────────────

        private void Case1_InitialState()
        {
            var response = new ApiResponseModel<ItemDto>();

            Assert.AreEqual(false, response.IsLoading.Value);
            Assert.AreEqual(false, response.HasError.Value);
            Assert.IsNull(response.Error.Value);
            Assert.IsNull(response.Data.Value);
            Assert.AreEqual(false, response.IsDisposed);

            response.Dispose();
            Assert.AreEqual(true, response.IsDisposed);
        }

        // ── Case 2: 요청 중 IsLoading=true, 완료 후 false ───────────────

        private void Case2_LoadingTransition()
        {
            var fake = new FakeHttpClient();
            fake.ReturnsDeferred<ItemDto>(out var trigger);

            var api = new ApiModel<Unit, ItemDto>(fake, "/api/item", HttpMethod.GET);

            // 요청 전 — Loading 아님
            Assert.AreEqual(false, api.Response.IsLoading.Value);

            // 요청 시작 (FakeHttpClient가 Subject를 들고 있으므로 아직 응답 없음)
            api.Request.Send(Unit.Default);

            // 응답 대기 중 — IsLoading=true
            Assert.AreEqual(true, api.Response.IsLoading.Value);

            // 응답 전달
            trigger.OnNext(new HttpResponse<ItemDto>
            {
                StatusCode = 200,
                Data       = new ItemDto { Id = "item_1" },
            });
            trigger.OnCompleted();

            // 응답 완료 후 — IsLoading=false
            Assert.AreEqual(false, api.Response.IsLoading.Value);
            Assert.AreEqual("item_1", api.Response.Data.Value.Id);

            api.Dispose();
        }

        // ── Case 3: 에러 후 성공 요청 → Error/HasError 초기화 ────────────

        private void Case3_ErrorClearedOnSuccess()
        {
            var fake = new FakeHttpClient();
            fake.Throws(HttpErrorKind.Network);
            fake.Returns(new ItemDto { Id = "item_ok" });

            var api = new ApiModel<Unit, ItemDto>(fake, "/api/item", HttpMethod.GET);

            // 에러 발생
            api.Request.Send(Unit.Default);
            Assert.AreEqual(true, api.Response.HasError.Value);
            Assert.IsNotNull(api.Response.Error.Value);

            // 성공 → Error 초기화
            api.Request.Send(Unit.Default);
            Assert.AreEqual(false, api.Response.HasError.Value);
            Assert.IsNull(api.Response.Error.Value);
            Assert.AreEqual("item_ok", api.Response.Data.Value.Id);

            api.Dispose();
        }

        // ── Case 4: 성공 후 에러 → Data는 이전 값 유지 ──────────────────
        // (에러가 발생해도 Data를 null로 초기화하지 않아야 합니다)

        private void Case4_DataPreservedOnError()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new ItemDto { Id = "item_prev" });
            fake.Throws(HttpErrorKind.ServerError, 500);

            var api = new ApiModel<Unit, ItemDto>(fake, "/api/item", HttpMethod.GET);

            // 성공으로 Data 설정
            api.Request.Send(Unit.Default);
            Assert.AreEqual("item_prev", api.Response.Data.Value.Id);

            // 에러 발생 — Data는 이전 값 유지
            api.Request.Send(Unit.Default);
            Assert.AreEqual(true, api.Response.HasError.Value);
            Assert.AreEqual("item_prev", api.Response.Data.Value.Id);  // 이전 데이터 보존

            api.Dispose();
        }

        // ── Case 5: PropModel 구독이 올바르게 동작하는지 ─────────────────

        private void Case5_PropModelSubscription()
        {
            var fake = new FakeHttpClient();
            fake.Returns(new ItemDto { Id = "subscribed_item" });

            var api = new ApiModel<Unit, ItemDto>(fake, "/api/item", HttpMethod.GET);

            // Data PropModel을 외부에서 구독
            string observedId = null;
            api.Response.Data.Subscribe(dto => observedId = dto?.Id).AddTo(disposables);

            api.Request.Send(Unit.Default);

            Assert.AreEqual("subscribed_item", observedId);

            // IsLoading PropModel 구독
            var loadingHistory = new System.Collections.Generic.List<bool>();
            var fake2 = new FakeHttpClient();
            fake2.ReturnsDeferred<ItemDto>(out var trigger2);

            var api2 = new ApiModel<Unit, ItemDto>(fake2, "/api/item", HttpMethod.GET);
            api2.Response.IsLoading.Subscribe(v => loadingHistory.Add(v)).AddTo(disposables);

            api2.Request.Send(Unit.Default);

            Assert.AreEqual(2, loadingHistory.Count);   // false(초기) → true(요청중)
            Assert.AreEqual(false, loadingHistory[0]);
            Assert.AreEqual(true, loadingHistory[1]);

            trigger2.OnNext(new HttpResponse<ItemDto> { StatusCode = 200, Data = new ItemDto { Id = "x" } });
            trigger2.OnCompleted();

            Assert.AreEqual(3, loadingHistory.Count);   // false(완료)
            Assert.AreEqual(false, loadingHistory[2]);

            api.Dispose();
            api2.Dispose();
        }
    }
}
