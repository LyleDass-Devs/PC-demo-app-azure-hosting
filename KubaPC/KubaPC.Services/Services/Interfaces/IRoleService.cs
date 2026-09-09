using KubaPC.Models.Contracts.Common.Responses;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Services.Interfaces
{
    public interface IRoleService
    {
        Task<List<DropDownResponse>> SelectAllActive();
    }
}
