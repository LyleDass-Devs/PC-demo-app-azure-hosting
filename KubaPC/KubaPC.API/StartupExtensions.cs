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
            // Register different named services directly with Autofac
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

            builder.Services.AddSingleton(appSettings);   // lyle - Autofac handles other services,helpers, if named differntly add service here
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
            });
        }

        public static void BuildDatabaseLogging(this WebApplicationBuilder builder)
        {
            builder.Services.AddSingleton<ILoggerProvider, DBErrorLoggerProvider>();
        }
    }
}
