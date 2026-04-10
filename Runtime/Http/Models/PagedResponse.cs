using System.Collections.Generic;

namespace Sindy.Http
{
    /// <summary>페이지네이션 API 응답 DTO.</summary>
    public class PagedResponse<T>
    {
        public List<T> Items      { get; set; } = new();
        public int     Page       { get; set; }
        public int     TotalPages { get; set; }
    }
}
