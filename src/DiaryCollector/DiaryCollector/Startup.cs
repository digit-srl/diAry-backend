using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using System;
using System.Text.Json;

namespace DiaryCollector {

    public class Startup {

        public Startup(IConfiguration configuration) {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public const string AdminUserLoginPolicy = "AdminUserLoginPolicy";

        public void ConfigureServices(IServiceCollection services) {
            services
                .AddControllersWithViews()
                .AddJsonOptions(opts => {
                    opts.JsonSerializerOptions.AllowTrailingCommas = true;
                    opts.JsonSerializerOptions.DictionaryKeyPolicy = JsonNamingPolicy.CamelCase;
                    opts.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
                    opts.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(CookieAuthenticationDefaults.AuthenticationScheme, opts => {
                    opts.LoginPath = "/user/login";
                    opts.LogoutPath = "/user/logout";
                    opts.ReturnUrlParameter = "return";
                    opts.ExpireTimeSpan = TimeSpan.FromDays(30);
                    opts.Cookie = new CookieBuilder {
                        IsEssential = true,
                        Name = "AuthLogin",
                        SecurePolicy = CookieSecurePolicy.Always,
                        SameSite = SameSiteMode.Strict,
                        HttpOnly = true
                    };
                })
            ;
            services.AddAuthorization(opts => {
                opts.AddPolicy(
                    AdminUserLoginPolicy,
                    new AuthorizationPolicyBuilder()
                        .RequireAuthenticatedUser()
                        .AddAuthenticationSchemes(CookieAuthenticationDefaults.AuthenticationScheme)
                        .Build()
                );
            });

            // Configuration
            services.Configure<ApiConfiguration>(Configuration.GetSection("Api"));

            // Components
            services.AddSingleton<MongoConnector>();
            services.AddSingleton<RequireApiKeyAttribute>();
            services.AddSingleton<WomService>();
        }

        public void Configure(
            IApplicationBuilder app,
            IWebHostEnvironment env,
            ILogger<Startup> logger
        ) {
            if (env.IsDevelopment()) {
                app.UseDeveloperExceptionPage();
            }

            // Fix incoming base path for hosting behind proxy
            string basePath = Environment.GetEnvironmentVariable("ASPNETCORE_BASEPATH");
            if (!string.IsNullOrWhiteSpace(basePath)) {
                logger.LogInformation("Configuring server to run under base path '{0}'", basePath);

                app.UsePathBase(new PathString(basePath));
                app.Use(async (context, next) => {
                    context.Request.PathBase = basePath;
                    await next.Invoke();
                });
            }

            app.UseDefaultFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints => {
                endpoints.MapControllers();
            });

            logger.LogInformation("Application startup complete");
        }
    }
}
