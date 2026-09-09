using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Common.Responses
{
    public class PagedResponse<T>
    {
        public int Page { get; set; }
        public int PageSize { get; set; }
        public int PageCount { get; set; }
        public long ItemCount { get; set; }
        public List<T> Data { get; set; } = new();
    }
}
