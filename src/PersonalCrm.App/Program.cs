using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using PersonalCrm.App.Endpoints;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Storage;
using Serilog;

namespace PersonalCrm.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Serilog bootstrap so we capture early-startup failures.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console()
            .CreateBootstrapLogger();

        try
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseSerilog((ctx, services, cfg) => cfg
                .ReadFrom.Configuration(ctx.Configuration)
                .ReadFrom.Services(services)
                .Enrich.FromLogContext());

            ConfigureServices(builder);

            var app = builder.Build();

            // Apply pending migrations on startup (MVP behaviour — see ROADMAP.md v0.1).
            // In production this should be gated behind a flag.
            using (var scope = app.Services.CreateScope())
            {
                var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                await db.Database.MigrateAsync();
            }

            ConfigurePipeline(app);

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Personal CRM host terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config   = builder.Configuration;

        // --- Persistence: SQLite default ---
        var connStr = config.GetConnectionString("Default")
                      ?? "Data Source=./data/personal-crm.db";
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite(connStr, o => o.CommandTimeout(30)));

        // --- Attachment storage (Local by default, S3 opt-in) ---
        services.AddAttachmentStore(config);

        // --- Auth + workspace context ---
        services.AddPersonalCrmAuth();

        // --- Blazor Web App (Server + WASM hybrid) + MudBlazor ---
        services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();

        services.AddMudServices();

        // --- Auth (placeholder wiring — full setup lands in v0.2) ---
        services.AddAuthentication();
        services.AddAuthorization();

        // --- Observability ---
        services.AddHealthChecks();
    }

    private static void ConfigurePipeline(WebApplication app)
    {
        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/error");
            app.UseHsts();
        }

        app.UseSerilogRequestLogging();
        app.UseStaticFiles();
        app.UseRouting();
        app.UseAuthentication();
        app.UseAuthorization();
        app.UseAntiforgery();

        app.MapHealthChecks("/healthz");

        // First vertical slice — setup + auth + contacts inside a workspace.
        app.MapAuthEndpoints();
        app.MapContactEndpoints();
        app.MapInteractionEndpoints();

        app.MapRazorComponents<App.Components.App>()
           .AddInteractiveServerRenderMode()
           .AddInteractiveWebAssemblyRenderMode();
    }
}
