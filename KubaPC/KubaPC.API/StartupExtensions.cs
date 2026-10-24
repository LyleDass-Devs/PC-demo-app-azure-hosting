using Autofac;
using KubaPC.API.Factories;
using KubaPC.API.Providers;
using KubaPC.Models.Internal;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace KubaPC.API
{
    public static class StartupExtensions
    {
        public const string CLIENT_CORS_POLICY = "KubaPCClients";
        public static void RunTimeEnvironment(this WebApplication app)
        {
            if (app.Environment.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }
        }

        public static void ConfigureContainer(ContainerBuilder builder)
        {

            builder.RegisterModule(new Factories.Services());
        }

        public static void BuildAuthentication(this WebApplicationBuilder builder)
        {
            AppSettingsModel appSettings = new AppSettingsModel();
            builder.Configuration.GetSection("AppSettings").Bind(appSettings);

            if (string.IsNullOrWhiteSpace(appSettings.Secret))
            {
                throw new InvalidOperationException("AppSettings:Secret is not configured.");
            }

            builder.Services.AddSingleton(appSettings);
            builder.Services.AddHttpContextAccessor();

            byte[] key = Encoding.ASCII.GetBytes(appSettings.Secret);

            builder.Services.AddAuthentication(x =>
            {
                x.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                x.DefaultScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(x =>
            {
                x.RequireHttpsMetadata = false;
                x.SaveToken = true;
                x.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuerSigningKey = true,
                    IssuerSigningKey = new SymmetricSecurityKey(key),
                    ValidateIssuer = true,
                    ValidIssuer = appSettings.Issuer,
                    ValidateAudience = true,
                    ValidAudience = appSettings.Audience,
                    ValidateLifetime = true,
                    ClockSkew = TimeSpan.Zero
                };
            });
        }

        public static void BuildAPIAuthorization(this WebApplicationBuilder builder)
        {
            builder.Services.AddAuthorization(o =>
            {
                o.AddPolicy("SuperAdmin", p => p.RequireRole("SuperAdmin"));
                o.AddPolicy("Admin", p => p.RequireRole("SuperAdmin", "Admin"));
                o.AddPolicy("Operator Staff", p => p.RequireRole("SuperAdmin", "Operator Staff"));
                o.AddPolicy("Monitor", p => p.RequireRole("SuperAdmin", "Monitor"));
                o.AddPolicy("Authoriser", p => p.RequireRole("SuperAdmin", "Authoriser"));
                o.AddPolicy("DOT", p => p.RequireRole("SuperAdmin", "DOT"));
                o.AddPolicy("Capture", p => p.RequireRole("SuperAdmin", "Operator Staff", "Monitor"));
                o.AddPolicy("Reader", p => p.RequireRole("SuperAdmin", "Admin", "Operator Staff", "Monitor", "Authoriser", "DOT"));
            });
        }

        public static void BuildCors(this WebApplicationBuilder builder)
        {
            string[] origins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? Array.Empty<string>();

            if (origins.Length == 0)
            {
                throw new InvalidOperationException("Cors:Origins is not configured. The Blazor clients cannot call the API without it.");
            }

            builder.Services.AddCors(options =>
            {
                options.AddPolicy(CLIENT_CORS_POLICY, policy => policy
                .WithOrigins(origins)
                .WithMethods("GET", "POST", "PUT", "OPTIONS")
                .WithHeaders("Authorization", "Content-Type")
                .WithExposedHeaders("Authorization")
                .SetPreflightMaxAge(TimeSpan.FromMinutes(10)));
            });
        }

        public static void BuildDatabaseLogging(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, DBErrorLoggerProvider>();
        }
    }
}
