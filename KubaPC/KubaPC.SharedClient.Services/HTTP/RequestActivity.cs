using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.SharedClient.Services.Http
{
    public class RequestActivity : IRequestActivity
    {
        private int count;

        public int Count => count;
        public bool IsBusy => count > 0;
        public event Action? Changed;

        public IDisposable Begin()
        {
            count++;
            Changed?.Invoke();
            return new Token(this);
        }

        private void End()
        {
            if (count == 0) return;
            count--;
            Changed?.Invoke();
        }

        private sealed class Token : IDisposable
        {
            private RequestActivity? owner;
            public Token(RequestActivity owner) => this.owner = owner;
            public void Dispose() { owner?.End(); owner = null; }
        }
    }  
}
