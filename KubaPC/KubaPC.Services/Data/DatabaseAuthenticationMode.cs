using System;

using Microsoft.Extensions.Configuration;

namespace KubaPC.Services.Data
{
    /// <summary>
    /// How the API proves who it is to PostgreSQL.
    /// </summary>
    public enum DatabaseAuthenticationMode
    {
        /// <summary>
        /// The connection string carries a user name and password. Used on developer machines.
        /// </summary>
        Password,

        /// <summary>
        /// The connection string carries only a user name; the password is a short-lived
        /// Microsoft Entra ID token fetched from the hosting platform's managed identity.
        /// </summary>
        EntraId
    }

    /// <summary>
    /// Decides the <see cref="DatabaseAuthenticationMode"/> from configuration. Pure, so it is unit tested
    /// without a database or a cloud identity.
    /// </summary>
    public static class DatabaseAuthenticationModeResolver
    {
        /// <summary>
        /// Configuration key whose value "true" (any casing) selects <see cref="DatabaseAuthenticationMode.EntraId"/>.
        /// Set as the App Service setting <c>Database__UseEntraIdAuthentication</c> by the infrastructure templates.
        /// </summary>
        public const string CONFIGURATION_KEY = "Database:UseEntraIdAuthentication";

        /// <summary>
        /// Returns <see cref="DatabaseAuthenticationMode.EntraId"/> when <see cref="CONFIGURATION_KEY"/> is "true"
        /// ignoring case; <see cref="DatabaseAuthenticationMode.Password"/> for any other value, an empty value,
        /// or a missing key.
        /// </summary>
        /// <param name="configuration">The application configuration to read the flag from.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="configuration"/> is null.</exception>
        public static DatabaseAuthenticationMode Resolve(IConfiguration configuration)
        {
            ArgumentNullException.ThrowIfNull(configuration);

            string? flagValue = configuration[CONFIGURATION_KEY];

            return string.Equals(flagValue, bool.TrueString, StringComparison.OrdinalIgnoreCase)
                ? DatabaseAuthenticationMode.EntraId
                : DatabaseAuthenticationMode.Password;
        }
    }
}
