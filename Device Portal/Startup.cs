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
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
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

                options.Events = new OpenIdConnectEvents()
                {
                    // Create or update user info in db after login
                    OnUserInformationReceived = async (context) => 
                    {
                        var db = context.HttpContext.RequestServices.GetRequiredService<PortalContext>();
                        var departmentService = context.HttpContext.RequestServices.GetRequiredService<DepartmentService>();

                        string userName = null;
                        if (context.User.RootElement.TryGetProperty("uids", out var value))
                        {
                            userName = value[0].GetString();
                        }
                        if (string.IsNullOrEmpty(userName)) { return; }
                        
                        int facultyId = db.Faculties.Select(f => f.Id).First();
                        var departmentMap = db.Departments.ToDictionary(d => d.Name);
                        var departmentRights = await departmentService.GetDepartments(userName);

                        var user = await db.Users.FindAsync(userName);
                        if (user == null)
                        {
                            string givenName = context.User.RootElement.TryGetProperty("given_name", out value) ?
                                value.GetString() : "";
                            string familyName = context.User.RootElement.TryGetProperty("family_name", out value) ?
                                value.GetString() : "";

                            user = new User()
                            {
                                FacultyId = facultyId,
                                Name = $"{givenName} {familyName}".Trim(),
                                UserName = userName,
                            };

                            foreach (var right in departmentRights)
                            {
                                var userDepartment = new User_Department()
                                {
                                    UserName = user.UserName,                                    
                                    CanManage = right.IsManager,
                                };
                                if (departmentMap.TryGetValue(right.Name, out var department))
                                {
                                    userDepartment.DepartmentId = department.Id;   
                                }
                                else
                                {
                                    userDepartment.Department = new Department { Name = right.Name, FacultyId = facultyId };
                                    departmentMap.Add(right.Name, userDepartment.Department);
                                }                                
                                user.Departments.Add(userDepartment);
                            }
                            db.Users.Add(user);
                        }
                        else 
                        {
                            var rightsMap = departmentRights.ToDictionary(r => r.Name);

                            // Update or remove current department from user
                            var userDepartments = db.Users_Departments
                                .Include(ud => ud.Department)
                                .Where(ud => ud.UserName == userName)
                                .ToArray();
                            foreach (var ud in userDepartments)
                            {
                                if (rightsMap.TryGetValue(ud.Department.Name, out var right))
                                {
                                    ud.CanManage = right.IsManager;
                                }
                                else { db.Users_Departments.Remove(ud); }
                            }

                            // Add new departments to user
                            var currentDepartments = userDepartments.Select(ud => ud.Department.Name).ToHashSet();
                            foreach (var right in departmentRights) 
                            {
                                if (!currentDepartments.Contains(right.Name))
                                {
                                    var user_department = new User_Department()
                                    {
                                        UserName = user.UserName,
                                        CanManage = right.IsManager,
                                    };
                                    if (departmentMap.TryGetValue(right.Name, out var department))
                                    {
                                        user_department.DepartmentId = department.Id;
                                    }
                                    else
                                    {
                                        user_department.Department = new Department { Name = right.Name, FacultyId = facultyId };
                                        departmentMap.Add(right.Name, user_department.Department);
                                    }
                                    db.Users_Departments.Add(user_department);
                                }
                            }
                        }

                    
                        await db.SaveChangesAsync();
                    }
                };
            });

            services.AddAuthorization(options => 
            {
                options.AddPolicy(AppPolicies.AdminOnly, 
                    policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanAdmin));
                options.AddPolicy(AppPolicies.ApproverOnly, 
                    policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanApprove));
                options.AddPolicy(AppPolicies.AuthorizedOnly, 
                    policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanSecure));
                options.AddPolicy(AppPolicies.ManagerOnly, 
                    policy => policy.RequireClaim(AppClaimTypes.Permission, AppClaims.CanManage));
                options.AddPolicy(AppPolicies.SecurityCheckAccess,
                    policy => policy.RequireAssertion(context => context.User.HasClaim(AppClaimTypes.Permission, AppClaims.CanSecure) ||
                                                                 context.User.HasClaim(AppClaimTypes.Permission, AppClaims.CanApprove)));
            });

            string clientId = Configuration["AzureAD:ClientID"];
            string clientSecret = Configuration["AzureAD:ClientSecret"];
            string tentantId = Configuration["AzureAD:TenantID"]; ;
            var confidentialClientApplication = ConfidentialClientApplicationBuilder
                .Create(clientId)
                .WithTenantId(tentantId)
                .WithClientSecret(clientSecret)
                .Build();
            IntuneService.authProvider = new ClientCredentialProvider(confidentialClientApplication);

            services.AddDbContext<PortalContext>(options =>
            {
                options.EnableSensitiveDataLogging();
                options.UseSqlServer(Configuration.GetConnectionString("DefaultConnection"), so => so.EnableRetryOnFailure());
            });

            services.AddHttpContextAccessor();
            services.AddControllers();
            services.AddRazorPages();
            services.AddHttpClient();
            services.AddScoped<IntuneService>();
            services.AddScoped<DepartmentService>();
            services.AddHostedService<NotificationService>();
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
            private readonly IHttpContextAccessor _httpContext;

            public ClaimsTransformer(PortalContext context, IHttpContextAccessor httpContext) 
            {
                _context = context;
                _httpContext = httpContext;
            }

            public async Task<ClaimsPrincipal> TransformAsync(ClaimsPrincipal principal) 
            {
                var userId = principal.GetUserName();
                var user = await _context.Users
                    .Where(u => u.UserName == userId)
                    .Select(u => new 
                    {
                        u.CanAdmin,
                        u.CanApprove,
                        CanManage = u.Departments.Any(d => d.CanManage),
                        u.CanSecure,
                        u.Name,
                    })
                    .SingleAsync();

                var identity = (ClaimsIdentity)principal.Identity;                
                if (user.CanAdmin) 
                {
                    string impersonationId = _httpContext.HttpContext.Request.Cookies["DevicePortal_Impersonate"];
                    if (!string.IsNullOrEmpty(impersonationId)) 
                    {
                        var userImpersonate = await _context.Users
                            .Where(u => u.UserName == impersonationId)
                            .Select(u => new
                            {                            
                                u.CanAdmin,
                                u.CanApprove,
                                CanManage = u.Departments.Any(d => d.CanManage),
                                u.CanSecure,
                                u.Name,
                            })
                            .FirstOrDefaultAsync();
                        if (userImpersonate != null) 
                        {
                            user = userImpersonate;
                            identity.AddClaim(new Claim(AppClaimTypes.Impersonation, "true"));

                            var claim = identity.Claims.FirstOrDefault(c => c.Type == "uids");
                            identity.RemoveClaim(claim);
                            identity.AddClaim(new Claim(claim.Type, impersonationId));

                            if (!string.IsNullOrEmpty(user.Name)) 
                            {
                                string[] parts = user.Name.Split(' ');
                                claim = identity.Claims.FirstOrDefault(c => c.Type == "given_name");
                                identity.RemoveClaim(claim);
                                identity.AddClaim(new Claim(claim.Type, parts[0]));

                                claim = identity.Claims.FirstOrDefault(c => c.Type == "family_name");
                                identity.RemoveClaim(claim);
                                identity.AddClaim(new Claim(claim.Type, parts.Length > 1 ? parts[1] : "Impersonate"));
                            }
                        }
                    }
                }
                
                if (user.CanSecure || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanSecure)); }
                if (user.CanApprove || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanApprove)); }
                if (user.CanManage || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanManage)); }
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
        public const string ManagerOnly = "ManagerOnly";

        public const string SecurityCheckAccess = "SecurityCheckAccess";
    }
    public static class AppClaims
    {
        public const string CanSecure = "CanSecure";
        public const string CanApprove = "CanApprove";
        public const string CanManage = "CanManage";
        public const string CanAdmin = "CanAdmin";
    }
    public static class AppClaimTypes
    {
        public static string Permission = "https://secure.datanose.nl/claims/permission";
        public static string Impersonation = "https://secure.datanose.nl/claims/impersonation";
    }
}
