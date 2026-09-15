using MotorPortal.API.Extensions;
using MotorPortal.API.Middleware;
using QuestPDF.Infrastructure;
using Serilog;

QuestPDF.Settings.License = LicenseType.Community;

Log.Logger = new LoggerConfiguration()
    .Enrich.FromLogContext()
    .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter())
    .CreateBootstrapLogger();

try
{
    var builder = WebApplication.CreateBuilder(args);

    Directory.CreateDirectory(Path.Combine(builder.Environment.ContentRootPath, "wwwroot", "certificates"));

    builder.Host.UseSerilog((context, services, configuration) =>
    {
        configuration
            .ReadFrom.Configuration(context.Configuration)
            .ReadFrom.Services(services)
            .Enrich.FromLogContext()
            .WriteTo.Console(new Serilog.Formatting.Json.JsonFormatter());
    });

    builder.Services.AddControllers();
    builder.Services.AddEndpointsApiExplorer();

    builder.Services.AddMotorPortalDatabase(builder.Configuration);
    builder.Services.AddMotorPortalAuthentication(builder.Configuration);
    builder.Services.AddMotorPortalCors();
    builder.Services.AddMotorPortalSwagger();
    builder.Services.AddMotorPortalRepositories();
    builder.Services.AddMotorPortalApplicationServices();

    var app = builder.Build();

    app.UseMiddleware<ExceptionHandlingMiddleware>();

    app.UseSerilogRequestLogging();

    if (app.Environment.IsDevelopment())
    {
        app.UseSwagger();
        app.UseSwaggerUI();
    }

    app.UseHttpsRedirection();

    app.UseStaticFiles();

    app.UseCors(ServiceCollectionExtensions.CorsPolicy);

    app.UseAuthentication();
    app.UseAuthorization();

    app.MapControllers();

    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "MotorPortal API terminated unexpectedly.");
}
finally
{
    Log.CloseAndFlush();
}
