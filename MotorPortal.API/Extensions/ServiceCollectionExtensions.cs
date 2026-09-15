using System.Net;
using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using MotorPortal.Application.Interfaces;
using MotorPortal.Infrastructure.Data;
using MotorPortal.Infrastructure.Repositories;
using MotorPortal.Infrastructure.Services;

namespace MotorPortal.API.Extensions;

public static class ServiceCollectionExtensions
{
    private const string CorsPolicyName = "MotorPortalWebPolicy";

    public static IServiceCollection AddMotorPortalDatabase(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection")
            ?? throw new InvalidOperationException("Connection string 'DefaultConnection' is not configured.");

        services.AddDbContext<AppDbContext>(options => options.UseNpgsql(connectionString));

        return services;
    }

    public static IServiceCollection AddMotorPortalAuthentication(this IServiceCollection services, IConfiguration configuration)
    {
        services.Configure<JwtSettings>(configuration.GetSection(JwtSettings.SectionName));

        var jwtSection = configuration.GetSection(JwtSettings.SectionName);
        var issuer = jwtSection["Issuer"] ?? throw new InvalidOperationException("Jwt:Issuer is not configured.");
        var audience = jwtSection["Audience"] ?? throw new InvalidOperationException("Jwt:Audience is not configured.");
        var signingKey = jwtSection["SigningKey"] ?? throw new InvalidOperationException("Jwt:SigningKey is not configured.");

        services.AddAuthentication(options =>
        {
            options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
            options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
        })
        .AddJwtBearer(options =>
        {
            options.TokenValidationParameters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true,
                ValidIssuer = issuer,
                ValidAudience = audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(signingKey)),
                ClockSkew = TimeSpan.FromMinutes(1)
            };
        });

        services.AddAuthorization();

        return services;
    }

    public static IServiceCollection AddMotorPortalCors(this IServiceCollection services)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(CorsPolicyName, policy =>
            {
                // Local dev: allow localhost/127.0.0.1 plus any private-LAN origin (e.g. a
                // phone on the same Wi-Fi hitting http://192.168.x.x:4795), regardless of
                // port or scheme, since the WEB app's dev-server port/host can vary.
                policy.SetIsOriginAllowed(origin =>
                    {
                        if (!Uri.TryCreate(origin, UriKind.Absolute, out var uri))
                        {
                            return false;
                        }

                        if (uri.Host is "localhost" or "127.0.0.1")
                        {
                            return true;
                        }

                        return IPAddress.TryParse(uri.Host, out var ip) && IsPrivateNetworkAddress(ip);
                    })
                    .AllowAnyHeader()
                    .AllowAnyMethod();
            });
        });

        return services;
    }

    public static string CorsPolicy => CorsPolicyName;

    private static bool IsPrivateNetworkAddress(IPAddress ip)
    {
        if (ip.IsIPv4MappedToIPv6)
        {
            ip = ip.MapToIPv4();
        }

        var bytes = ip.GetAddressBytes();
        if (bytes.Length != 4)
        {
            return false;
        }

        return bytes[0] switch
        {
            10 => true,
            172 => bytes[1] is >= 16 and <= 31,
            192 => bytes[1] == 168,
            _ => false
        };
    }

    public static IServiceCollection AddMotorPortalSwagger(this IServiceCollection services)
    {
        services.AddSwaggerGen(options =>
        {
            options.SwaggerDoc("v1", new OpenApiInfo { Title = "MotorPortal API", Version = "v1" });

            var securityScheme = new OpenApiSecurityScheme
            {
                Name = "Authorization",
                Type = SecuritySchemeType.Http,
                Scheme = "bearer",
                BearerFormat = "JWT",
                In = ParameterLocation.Header,
                Description = "Enter a valid JWT token. Example: Bearer eyJhbGciOi...",
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            };

            options.AddSecurityDefinition("Bearer", securityScheme);
            options.AddSecurityRequirement(new OpenApiSecurityRequirement
            {
                { securityScheme, Array.Empty<string>() }
            });

            options.OperationFilter<MotorPortal.API.Swagger.FileUploadOperationFilter>();
        });

        return services;
    }

    public static IServiceCollection AddMotorPortalRepositories(this IServiceCollection services)
    {
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IUserRepository, UserRepository>();

        return services;
    }

    public static IServiceCollection AddMotorPortalApplicationServices(this IServiceCollection services)
    {
        services.AddScoped<IAuthService, AuthService>();
        services.AddScoped<IJwtTokenGenerator, JwtTokenGenerator>();
        services.AddScoped<IExcelBatchService, ExcelBatchService>();
        services.AddScoped<IBatchProcessingService, BatchProcessingService>();
        services.AddScoped<IPaymentTaggingService, PaymentTaggingService>();
        services.AddScoped<IPolicyCertificateService, PolicyCertificateService>();
        services.AddScoped<IPfGatewayService, MockPfService>();
        services.AddScoped<IBatchSummaryService, BatchSummaryService>();
        services.AddScoped<IMasterPolicyService, MasterPolicyService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IPolicyService, PolicyService>();

        return services;
    }
}
