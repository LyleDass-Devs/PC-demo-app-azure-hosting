using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services.Http
{
    public interface IRequestActivity
    {
        int Count { get; }
        bool IsBusy { get; }
        event Action? Changed;
        IDisposable Begin();
    }
}
