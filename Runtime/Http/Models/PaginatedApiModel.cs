using System;
using System.Collections.Generic;
using R3;
using Sindy.View;
using Sindy.View.Components;
using Sindy.View.Features;

namespace Sindy.Http
{
    /// <summary>
    /// 페이지네이션을 지원하는 API 모델.
    /// CurrentPage, TotalPages, Items를 PropModel로 노출하여 UI 바인딩이 가능합니다.
    /// PrevButton/NextButton으로 페이지 이동을 제어합니다.
    ///
    /// 사용 예:
    ///   var paged = new PaginatedApiModel&lt;RankingDto&gt;(client, "/api/ranking",
    ///       dto => { var vm = new ViewModel(); vm["name"] = new PropModel&lt;string&gt;(dto.Name); return vm; });
    ///   paged.GoToPage(1);
    /// </summary>
    public class PaginatedApiModel<TItem> : ViewModel
    {
        public PropModel<int>             CurrentPage { get; } = new(1);
        public PropModel<int>             TotalPages  { get; } = new(1);
        public PropModel<List<IViewModel>> Items      { get; } = new(new List<IViewModel>());
        public PropModel<bool>            IsLoading   { get; } = new(false);
        public PropModel<HttpError>       Error       { get; } = new();
        public PropModel<bool>            HasError    { get; } = new(false);

        public ButtonModel PrevButton { get; } = new();
        public ButtonModel NextButton { get; } = new();

        private readonly IHttpClient client;
        private readonly string baseUrl;
        private readonly Func<TItem, IViewModel> itemMapper;

        /// <param name="client">HTTP 클라이언트</param>
        /// <param name="baseUrl">기본 URL (?page=N이 추가됨)</param>
        /// <param name="itemMapper">DTO → IViewModel 변환 함수</param>
        public PaginatedApiModel(
            IHttpClient client,
            string baseUrl,
            Func<TItem, IViewModel> itemMapper)
        {
            this.client     = client;
            this.baseUrl    = baseUrl;
            this.itemMapper = itemMapper;

            PrevButton.With(new InteractableFeature(false));
            NextButton.With(new InteractableFeature(false));

            // 버튼 활성화 상태 연동
            Observable.CombineLatest(CurrentPage.Obs, TotalPages.Obs, (cur, total) => cur > 1)
                .Subscribe(v => PrevButton.Feature<InteractableFeature>().Interactable.Value = v)
                .AddTo(disposables);

            Observable.CombineLatest(CurrentPage.Obs, TotalPages.Obs, (cur, total) => cur < total)
                .Subscribe(v => NextButton.Feature<InteractableFeature>().Interactable.Value = v)
                .AddTo(disposables);

            PrevButton.Obs.Subscribe(_ => GoToPage(CurrentPage.Value - 1)).AddTo(disposables);
            NextButton.Obs.Subscribe(_ => GoToPage(CurrentPage.Value + 1)).AddTo(disposables);
        }

        public void GoToPage(int page)
        {
            if (page < 1) return;

            IsLoading.Value = true;
            HasError.Value  = false;
            Error.Value     = null;
            var url = $"{baseUrl}?page={page}";

            client.Get<PagedResponse<TItem>>(url)
                .Catch<HttpResponse<PagedResponse<TItem>>, Exception>(ex =>
                {
                    Error.Value     = ex as HttpError ?? new HttpError(0, ex.Message, HttpErrorKind.Network, ex);
                    HasError.Value  = true;
                    IsLoading.Value = false;
                    return Observable.Empty<HttpResponse<PagedResponse<TItem>>>();
                })
                .Subscribe(res =>
                {
                    CurrentPage.Value = res.Data.Page;
                    TotalPages.Value  = res.Data.TotalPages;
                    Items.Value       = BuildViewModels(res.Data.Items);
                    IsLoading.Value   = false;
                })
                .AddTo(disposables);
        }

        private List<IViewModel> BuildViewModels(List<TItem> items)
        {
            var result = new List<IViewModel>();
            foreach (var dto in items)
                result.Add(itemMapper(dto));
            return result;
        }

        public override void Dispose()
        {
            base.Dispose();
            CurrentPage.Dispose();
            TotalPages.Dispose();
            Items.Dispose();
            IsLoading.Dispose();
            Error.Dispose();
            HasError.Dispose();
            PrevButton.Dispose();
            NextButton.Dispose();
        }
    }
}
