using BCrypt.Net;
using System;
using System.Collections.Generic;
using System.Text;

namespace KubaPC.Services.Helpers
{
    public class PasswordHelper : IPasswordHelper
    {
        private const int WORK_FACTOR = 12;

        public string Hash(string password)
        {
            return BCrypt.Net.BCrypt.HashPassword(password, WORK_FACTOR);
        }

        public bool Verify(string password, string? storedHash)
        {
            if (string.IsNullOrWhiteSpace(storedHash))
            {
                return false;
            }

            return BCrypt.Net.BCrypt.Verify(password, storedHash);
        }
    }
}
