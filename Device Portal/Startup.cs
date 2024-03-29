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
using Microsoft.Extensions.Options;
using Microsoft.Graph.Auth;
using Microsoft.Identity.Client;
using Microsoft.IdentityModel.Protocols.OpenIdConnect;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Net;
using System.Security.Claims;
using System.Text.Encodings.Web;
using System.Text.Json;
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
                
        public void ConfigureServices(IServiceCollection services)
        {
            #if DEBUG
            services.AddCors();
            #endif

            JwtSecurityTokenHandler.DefaultMapInboundClaims = false;
            services.AddTransient<IClaimsTransformation, ClaimsTransformer>();
            services.AddAuthentication(options =>
            {
                options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = OpenIdConnectDefaults.AuthenticationScheme;
            })
            .AddApiKey(options => { })
            .AddCookie(options => 
            {
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.SlidingExpiration = true;
                options.ExpireTimeSpan = TimeSpan.FromMinutes(15);
            })
            .AddOpenIdConnect(options =>
            {
                options.SignInScheme = CookieAuthenticationDefaults.AuthenticationScheme;
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
                        var rights = await departmentService.GetDepartments(userName); // contains duplicates
                        var rightsMap = new Dictionary<string, DepartmentService.Department>();
                        foreach (var right in rights) { rightsMap.TryAdd(right.Name, right); }

                        var user = await db.Users.FindAsync(userName);
                        if (user == null)
                        {
                            string givenName = context.User.RootElement.TryGetProperty("given_name", out value) ?
                                value.GetString() : "";
                            string familyName = context.User.RootElement.TryGetProperty("family_name", out value) ?
                                value.GetString() : "";
                            string email = context.User.RootElement.TryGetProperty("email", out value) ? value.GetString() : "";

                            user = new User()
                            {
                                FacultyId = facultyId,
                                Name = $"{givenName} {familyName}".Trim(),
                                UserName = userName,
                                Email = email,
                            };

                            foreach (var right in rightsMap.Values)
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
                            if (string.IsNullOrEmpty(user.Email))
                            {
                                string email = context.User.RootElement.TryGetProperty("email", out value) ? value.GetString() : "";
                                user.Email = email;
                                var entry = db.Entry(user);
                                entry.Property(u => u.Email).IsModified = true;
                            }
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
                            foreach (var right in rightsMap.Values) 
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

            ApiKeyAuthenticationHandler.apiKey = Configuration["Api:Key"];

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
            services.AddSingleton<NotificationTask>();
            services.AddHostedService<NotificationTask>();
            services.AddSingleton<RightsTask>();
            services.AddHostedService<RightsTask>();
            services.AddSingleton<ExpireTask>();
            services.AddHostedService<ExpireTask>();
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
            
            #if DEBUG
            app.UseCors(options =>
            {
                options.AllowAnyOrigin();
                options.AllowAnyMethod();
                options.AllowAnyHeader();
                options.SetIsOriginAllowed(_ => true);
            });
            #endif

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
                        CanManageFaculty = u.Departments.Any(d => d.CanManage && d.Department.Name == "FNWI"),
                        u.Name,
                    })
                    .SingleOrDefaultAsync();
                if (user == null) { return principal; }

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
                                CanManageFaculty = u.Departments.Any(d => d.CanManage && d.Department.Name == "FNWI"),
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
                                string firstName = parts[0];
                                string lastName = parts.Length > 1 ? string.Join(' ', parts[1..]) : "Impersonate";

                                claim = identity.Claims.FirstOrDefault(c => c.Type == "given_name");
                                identity.RemoveClaim(claim);
                                identity.AddClaim(new Claim(claim.Type, parts[0]));

                                claim = identity.Claims.FirstOrDefault(c => c.Type == "family_name");
                                identity.RemoveClaim(claim);
                                identity.AddClaim(new Claim(claim.Type, lastName));
                            }
                        }
                    }
                }
                
                if (user.CanSecure || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanSecure)); }
                if (user.CanApprove || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanApprove)); }
                if (user.CanManage || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanManage)); }
                if (user.CanManageFaculty || user.CanAdmin) { identity.AddClaim(new Claim(AppClaimTypes.Permission, AppClaims.CanManageFaculty)); }
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
        public const string CanManageFaculty = "CanManageFaculty";
        public const string CanAdmin = "CanAdmin";
    }
    public static class AppClaimTypes
    {
        public static string Permission = "https://secure.datanose.nl/claims/permission";
        public static string Impersonation = "https://secure.datanose.nl/claims/impersonation";
    }

    // https://josef.codes/asp-net-core-protect-your-api-with-api-keys/
    public class ApiKeyAuthenticationOptions : AuthenticationSchemeOptions
    {
        public const string DefaultScheme = "API Key";
        public string Scheme => DefaultScheme;
        public string AuthenticationType = DefaultScheme;
    }

    public static class AuthenticationBuilderExtensions
    {
        public static AuthenticationBuilder AddApiKey(this AuthenticationBuilder authenticationBuilder, Action<ApiKeyAuthenticationOptions> options)
        {
            return authenticationBuilder.AddScheme<ApiKeyAuthenticationOptions, ApiKeyAuthenticationHandler>(ApiKeyAuthenticationOptions.DefaultScheme, options);
        }
    }

    public class ApiKeyAuthenticationHandler : AuthenticationHandler<ApiKeyAuthenticationOptions>
    {
        public static string apiKey;

        public ApiKeyAuthenticationHandler(
            IOptionsMonitor<ApiKeyAuthenticationOptions> options,
            ILoggerFactory logger,
            UrlEncoder encoder,
            ISystemClock clock) : base(options, logger, encoder, clock)
        {
        }

        protected override Task<AuthenticateResult> HandleAuthenticateAsync()
        {
            if (!Request.Headers.TryGetValue("X-Api-Key", out var apiKeyHeaderValues))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            var providedApiKey = apiKeyHeaderValues.FirstOrDefault();

            if (apiKeyHeaderValues.Count == 0 || string.IsNullOrWhiteSpace(providedApiKey))
            {
                return Task.FromResult(AuthenticateResult.NoResult());
            }

            if (apiKey != null && providedApiKey == apiKey)
            {
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.GivenName, "api"),
                    new Claim(AppClaimTypes.Permission, AppClaims.CanAdmin),
                    new Claim(AppClaimTypes.Permission, AppClaims.CanApprove),
                    new Claim(AppClaimTypes.Permission, AppClaims.CanManage),
                    new Claim(AppClaimTypes.Permission, AppClaims.CanManageFaculty),
                    new Claim(AppClaimTypes.Permission, AppClaims.CanSecure),
                };

                var identity = new ClaimsIdentity(claims, Options.AuthenticationType);
                var identities = new List<ClaimsIdentity> { identity };
                var principal = new ClaimsPrincipal(identities);
                var ticket = new AuthenticationTicket(principal, Options.Scheme);

                return Task.FromResult(AuthenticateResult.Success(ticket));
            }

            return Task.FromResult(AuthenticateResult.NoResult());
        }

        protected override async Task HandleChallengeAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 401;
            Response.ContentType = "application/problem+json";

            await Response.WriteAsync(JsonSerializer.Serialize(new {
                Type = "https://httpstatuses.com/401",
                Title = "Unauthorized",
                Status = 401,
            }));
        }

        protected override async Task HandleForbiddenAsync(AuthenticationProperties properties)
        {
            Response.StatusCode = 403;
            Response.ContentType = "application/problem+json";

            await Response.WriteAsync(JsonSerializer.Serialize(new {
                Type = "https://httpstatuses.com/403",
                Title = "Forbidden",
                Status = 403,
            }));
        }
    }

}
