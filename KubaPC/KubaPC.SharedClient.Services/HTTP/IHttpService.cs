using KubaPC.SharedClient.Services.Models;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services.Http
{
    public interface IHttpService
    {
        Task<ApiResult<T>> Get<T>(string uri);
        Task<ApiResult<T>> Post<T>(string uri, object body);
        Task<ApiResult> Post(string uri, object body);
        Task<ApiResult> Put(string uri, object body);
    }
}
