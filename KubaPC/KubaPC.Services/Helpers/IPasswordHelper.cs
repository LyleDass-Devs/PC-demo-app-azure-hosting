using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Helpers
{
    public interface IPasswordHelper
    {
        string Hash(string password);
        bool Verify(string password, string? storedHash);
    }
}
