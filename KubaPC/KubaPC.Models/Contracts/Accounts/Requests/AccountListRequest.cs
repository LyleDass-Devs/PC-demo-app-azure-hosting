using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Models.Contracts.Accounts.Requests
{
    public class AccountListRequest
    {
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public string? Search { get; set; }
        public bool? IsActive { get; set; }
        public string? SortBy { get; set; }
        public bool SortDescending { get; set; }
    }
}
