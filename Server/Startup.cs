using BlazorDemo.Server.Data;
using BlazorDemo.Server.Models;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.ResponseCompression;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using System;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;

namespace BlazorDemo.Server
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        // For more information on how to configure your application, visit https://go.microsoft.com/fwlink/?LinkID=398940
        public void ConfigureServices(IServiceCollection services)
        {
            services.AddDbContext<ApplicationDbContext>(options =>
                options.UseSqlServer(
                    Configuration.GetConnectionString("DefaultConnection")));

            services.AddDatabaseDeveloperPageExceptionFilter();

            services.AddDefaultIdentity<IdentityUser>(options => options.SignIn.RequireConfirmedAccount = true)
               .AddRoles<IdentityRole>()
               .AddEntityFrameworkStores<ApplicationDbContext>();

            services.AddIdentityServer()
                .AddApiAuthorization<IdentityUser, ApplicationDbContext>
                (options =>
                {
                    // Specify Redirect Uris for BlazorDemo.AuthorizedPwaClient..
                    options.Clients["BlazorDemo.AuthorizedPwaClient"].RedirectUris.Clear();
                    options.Clients["BlazorDemo.AuthorizedPwaClient"].PostLogoutRedirectUris.Clear();
                    options.Clients["BlazorDemo.AuthorizedPwaClient"].RedirectUris.Add("/authorizedpwa/authentication/login-callback");
                    options.Clients["BlazorDemo.AuthorizedPwaClient"].PostLogoutRedirectUris.Add("/authorizedpwa/authentication/logout-callback");

                    options.IdentityResources["openid"].UserClaims.Add("name");
                    options.ApiResources.Single().UserClaims.Add("name");
                    options.IdentityResources["openid"].UserClaims.Add("role");
                    options.ApiResources.Single().UserClaims.Add("role");
                });

            JwtSecurityTokenHandler.DefaultInboundClaimTypeMap.Remove("role");

            services.Configure<IdentityOptions>(options =>
                options.ClaimsIdentity.UserIdClaimType = ClaimTypes.NameIdentifier);

            services.AddAuthentication()
                .AddIdentityServerJwt();

            services.AddControllersWithViews();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                app.UseMigrationsEndPoint();
                app.UseWebAssemblyDebugging();
            }
            else
            {
                app.UseExceptionHandler("/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }

            app.UseHttpsRedirection();

            // Using Multiple Blazor Apps - https://docs.microsoft.com/en-us/aspnet/core/blazor/host-and-deploy/webassembly?view=aspnetcore-5.0#hosted-deployment-with-multiple-blazor-webassembly-apps
            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/Pwa", StringComparison.OrdinalIgnoreCase), first =>
            {
                first.UseBlazorFrameworkFiles("/Pwa");
                first.UseStaticFiles("/Pwa");
                first.UseStaticFiles();

                first.UseRouting();

                first.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapFallbackToFile("Pwa/{*path:nonfile}", "Pwa/index.html");
                });
            });

            app.MapWhen(ctx => ctx.Request.Path.StartsWithSegments("/AuthorizedPwa", StringComparison.OrdinalIgnoreCase), first =>
            {
                first.UseBlazorFrameworkFiles("/AuthorizedPwa");
                first.UseStaticFiles("/AuthorizedPwa");
                first.UseStaticFiles();

                first.UseRouting();

                first.UseIdentityServer();
                first.UseAuthentication();
                first.UseAuthorization();

                first.UseEndpoints(endpoints =>
                {
                    endpoints.MapControllers();
                    endpoints.MapFallbackToFile("AuthorizedPwa/{*path:nonfile}", "AuthorizedPwa/index.html");
                });
            });

            app.UseBlazorFrameworkFiles();
            app.UseStaticFiles();

            app.UseRouting();

            app.UseIdentityServer();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapRazorPages();
                endpoints.MapControllers();
                endpoints.MapFallbackToFile("index.html");
            });
        }
    }
}
