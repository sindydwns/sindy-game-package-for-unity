using Sindy.View;

namespace Sindy.Http
{
    /// <summary>
    /// HTTP 응답 상태를 PropModel로 노출하는 ViewModel.
    /// IsLoading / Data / Error / HasError 네 가지 상태를 제공합니다.
    /// 기존 PropModel과 동일하게 SindyComponent에서 SetModel()로 바인딩할 수 있습니다.
    ///
    /// 예)
    ///   loadingSpinner.SetModel(response.IsLoading).SetParent(this);
    ///   nameLabel.SetModel(response.Data.Select(dto => dto?.Name ?? "").ToPropModel()).SetParent(this);
    /// </summary>
    public class ApiResponseModel<T> : ViewModel
    {
        public PropModel<T>         Data      { get; } = new();
        public PropModel<bool>      IsLoading { get; } = new(false);
        public PropModel<HttpError> Error     { get; } = new();
        public PropModel<bool>      HasError  { get; } = new(false);

        public ApiResponseModel()
        {
            this["data"]      = Data;
            this["isLoading"] = IsLoading;
            this["error"]     = Error;
            this["hasError"]  = HasError;
        }

        internal void SetLoading()
        {
            IsLoading.Value = true;
            HasError.Value  = false;
            Error.Value     = null;
        }

        internal void SetSuccess(HttpResponse<T> response)
        {
            Data.Value      = response.Data;
            IsLoading.Value = false;
        }

        internal void SetError(HttpError error)
        {
            Error.Value     = error;
            HasError.Value  = true;
            IsLoading.Value = false;
        }

        public override void Dispose()
        {
            base.Dispose();
            Data.Dispose();
            IsLoading.Dispose();
            Error.Dispose();
            HasError.Dispose();
        }
    }
}
