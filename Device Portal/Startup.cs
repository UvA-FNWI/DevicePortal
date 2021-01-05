using DevicePortal.Controllers;
using DevicePortal.Data;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.OAuth.Claims;
using Microsoft.AspNetCore.Authentication.OpenIdConnect;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.HttpsPolicy;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Threading.Tasks;

namespace DevicePortal
{
    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddCookie(options => 
            {
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;

                // Local
                //options.Authority = "https://localhost:5001";
                //options.ClientId = "device_portal";
                //options.ClientSecret = "secret";

                // Surfconext test
                options.Prompt = "login";
                options.Authority = Configuration["OpenID:Authority"];
                options.ClientId = Configuration["OpenID:ClientID"];
                options.ClientSecret = Configuration["OpenID:ClientSecret"];
                options.Scope.Add("openid");
                options.GetClaimsFromUserInfoEndpoint = true;
                options.ClaimActions.Add(new JsonKeyClaimAction("uids", null, "uids"));
                options.ResponseType = OpenIdConnectResponseType.Code;
                options.SaveTokens = true;
            });

            services.AddAuthorization(options => 
            {
                options.AddPolicy(AppPolicies.AdminOnly, policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanAdmin));
                options.AddPolicy(AppPolicies.ApproverOnly, policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanApprove));
                options.AddPolicy(AppPolicies.AuthorizedOnly, policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanSecure));
            });

            string clientId = Configuration["AzureAD:ClientID"];
            string clientSecret = Configuration["AzureAD:ClientSecret"];
            string tentantId = Configuration["AzureAD:TenantID"]; ;
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tentantId)
                .WithClientSecret(clientSecret)
                .Build();
            IntuneController.authProvider = new ClientCredentialProvider(confidentialClientApplication);

            services.AddDbContext<PortalContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), so => so.EnableRetryOnFailure());
            });

            services.AddControllers();
            services.AddRazorPages();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }
            else
            {
                app.UseExceptionHandler("/Home/Error");
                // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
                app.UseHsts();
            }
            app.UseHttpsRedirection();
            app.UseStaticFiles();

            app.UseRouting();
            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers().RequireAuthorization();
                endpoints.MapRazorPages().RequireAuthorization();

                endpoints.MapFallbackToPage("/Index");
            });
        }

        class ClaimsTransformer : IClaimsTransformation
        {
            private readonly PortalContext _context;

            public ClaimsTransformer(PortalContext context) 
            {
                _context = context;
            }

            public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) 
            {
                var userId = principal.GetUserName();
                var user = await _context.Users.FindAsync(userId);
                if (user == null) 
                {
                    user = new User()
                    {
                        // TODO Get from datanose API
                        Faculty = "",
                        Institute = "",

                        Name = principal.GetFullName(),
                        UserName = userId,
                    };
                    _context.Users.Add(user);
                    await _context.SaveChangesAsync();
                }

                var identity = (ClaimsIdentity)principal.Identity;                
                if (user.CanSecure || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanSecure)); }
                if (user.CanApprove || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanApprove)); }
                if (user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanAdmin)); }
                return principal;
            }
        }
    }

    public static class AppPolicies
    {
        public const string AdminOnly = "AdminOnly";
        public const string ApproverOnly = "ApproverOnly";
        public const string AuthorizedOnly = "AuthorizedOnly";
    }
    public static class AppClaims
    {
        public const string CanSecure = "CanSecure";
        public const string CanApprove = "CanApprove";
        public const string CanAdmin = "CanAdmin";
    }
    public static class AppClaimTypes
    {
        public static string Permission = "https://secure.datanose.nl/claims/permission";
    }
}
