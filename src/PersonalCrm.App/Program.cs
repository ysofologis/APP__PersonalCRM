using Microsoft.AspNetCore.Http;
using Microsoft.EntityFrameworkCore;
using MudBlazor.Services;
using PersonalCrm.App.Auth;
using PersonalCrm.App.Endpoints;
using PersonalCrm.App.Services;
using PersonalCrm.Core.Domain;
using PersonalCrm.Infrastructure.Persistence;
using PersonalCrm.Infrastructure.Security;
using PersonalCrm.Infrastructure.Storage;
using Serilog;

namespace PersonalCrm.App;

public class Program
{
    public static async Task Main(string[] args)
    {
        // Serilog bootstrap so we capture early-startup failures.
        Log.Logger = new LoggerConfiguration()
            .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
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

                // Make sure the SQLite file's parent directory exists. SQLite
                // returns 'unable to open database file' (Error 14) if the
                // path's directory hasn't been created — even when the file
                // itself doesn't yet exist. The connection string has already
                // been absolute-resolved at registration time (see above).
                var connStr = db.Database.GetConnectionString();
                if (!string.IsNullOrWhiteSpace(connStr))
                {
                    var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr);
                    var dir = Path.GetDirectoryName(csb.DataSource);
                    if (!string.IsNullOrWhiteSpace(dir) && !Directory.Exists(dir))
                    {
                        Directory.CreateDirectory(dir);
                    }
                }

                await db.Database.MigrateAsync();

                // v0.1 convenience bootstrap: if no setup has run yet, seed a
                // default admin + workspace so the app is usable without the
                // /setup wizard. Idempotent — runs on every startup but only
                // inserts when the database is genuinely empty.
                //
                // Gated behind `Bootstrap:SeedDefault` so production
                // deployments can opt out via env var (e.g.
                // `Bootstrap__SeedDefault=false`). Defaults to true so the
                // out-of-the-box dev experience still works.
                var seedDefault =
                    builder.Configuration.GetValue("Bootstrap:SeedDefault", true);
                if (seedDefault)
                {
                    await SeedDefaultInstanceAsync(db);
                }
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

    /// <summary>
    /// Resolve the SQLite <c>Data Source</c> in <paramref name="connStr"/> to an
    /// absolute path against <paramref name="contentRoot"/>. Leaves in-memory
    /// databases (<c>:memory:</c>) and already-absolute paths alone.
    /// </summary>
    internal static string ResolveSqlitePath(string connStr, string contentRoot)
    {
        if (string.IsNullOrWhiteSpace(connStr))
        {
            return connStr;
        }

        var csb = new Microsoft.Data.Sqlite.SqliteConnectionStringBuilder(connStr);
        var src = csb.DataSource;

        if (string.IsNullOrWhiteSpace(src) || src == ":memory:")
        {
            return connStr;
        }

        if (Path.IsPathRooted(src))
        {
            return connStr;
        }

        csb.DataSource = Path.GetFullPath(Path.Combine(contentRoot, src));
        return csb.ConnectionString;
    }

    /// <summary>
    /// v0.1 convenience bootstrap: if no setup has run yet, insert a default
    /// admin user, a default workspace, and a default Instance row so the app
    /// is usable straight after the first boot — no /setup wizard needed.
    ///
    /// Idempotent: re-checks the watermark on every boot. Once any
    /// <see cref="User"/> exists, no seed runs.
    ///
    /// Defaults:
    ///   Site name : "Demo CRM"
    ///   Admin     : admin@example.com  /  11111111
    ///   Workspace : "Default Workspace"
    /// </summary>
    private static async Task SeedDefaultInstanceAsync(AppDbContext db)
    {
        if (await db.Users.AnyAsync())
        {
            return; // already bootstrapped — don't re-seed.
        }

        var now    = DateTimeOffset.UtcNow;
        var userId = Guid.NewGuid();
        var wsId   = Guid.NewGuid();

        var user = new User
        {
            Id              = userId,
            Email           = "admin@example.com",
            PasswordHash    = PasswordHasher.Hash("11111111"),
            DisplayName     = "Admin",
            IsInstanceAdmin = true,
            IsEmailVerified = true,
            CreatedAt       = now,
        };

        var workspace = new Workspace
        {
            Id         = wsId,
            Name       = "Default Workspace",
            IsPersonal = false,
            CreatedAt  = now,
        };

        var member = new WorkspaceMember
        {
            WorkspaceId = wsId,
            UserId      = userId,
            Role        = WorkspaceRole.Owner,
            JoinedAt    = now,
        };

        var instance = new Instance
        {
            SiteName         = "Demo CRM",
            DefaultLanguage  = "en-US",
            CreatedAt        = now,
            SetupCompletedAt = now,
        };

        db.Users.Add(user);
        db.Workspaces.Add(workspace);
        db.WorkspaceMembers.Add(member);
        db.Instances.Add(instance);

        await db.SaveChangesAsync();

        Log.Information(
            "Bootstrap: seeded admin=admin@example.com workspace=Default Workspace (userId={UserId}, workspaceId={WorkspaceId})",
            userId,
            wsId);
    }

    private static void ConfigureServices(WebApplicationBuilder builder)
    {
        var services = builder.Services;
        var config   = builder.Configuration;

        // --- Persistence: SQLite default ---
        //
        // Resolve the connection string's Data Source to an absolute path against
        // the app's content root before registering the DbContext. This avoids
        // the classic "relative path is resolved against different working
        // directories at different times" bug — migration runner opens the
        // file from one CWD, request handlers open it from another, and the
        // app silently queries a different SQLite file than it migrated.
        var connStr = config.GetConnectionString("Default")
                      ?? "Data Source=./data/personal-crm.db";
        var resolvedConnStr = ResolveSqlitePath(connStr, builder.Environment.ContentRootPath);
        services.AddDbContext<AppDbContext>(opt =>
            opt.UseSqlite(resolvedConnStr, sqlite =>
            {
                sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                sqlite.MigrationsHistoryTable("__EFMigrationsHistory");
            }));

        // --- Attachment storage (Local by default, S3 opt-in) ---
        services.AddAttachmentStore(config);

        // --- Auth + workspace context ---
        services.AddPersonalCrmAuth();

        // --- API client for Blazor pages (talks to /api/* over HttpClient) ---
        //
        // ApiClient itself consumes IHttpClientFactory + NavigationManager
        // (both scoped) and resolves the base address at the moment of each
        // call. So we just register the factory here.
        // A named client that never proxies. `AddHttpClient()`'s default
        // handler honours the HTTP_PROXY / HTTPS_PROXY environment variables,
        // and this dev host exports HTTP_PROXY=squid-proxy:3128. That made
        // every same-origin /api/* call from Blazor go out through Squid,
        // which cannot resolve *our* loopback and answered with a bare
        //   503 Service Unavailable / X-Squid-Error: ERR_CONNECT_FAIL
        // instead of reaching Kestrel. curl was unaffected because the shell
        // that ran it had no proxy variables set, which is what made this
        // look like a browser-only bug.
        services.AddHttpClient("app-internal")
                .ConfigurePrimaryHttpMessageHandler(() => new HttpClientHandler
                {
                    UseProxy = false,
                });
        services.AddHttpClient();

        services.AddScoped<ApiClient>();

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
        // Antiforgery is only meaningful for the Blazor Server interactive
        // circuit's `<form>` POSTs, which carry an antiforgery token emitted
        // by the framework. The /api/* JSON endpoints are CSRF-safe on their
        // own (SameSite=Lax + httpOnly refresh cookies, JSON-only bodies,
        // explicit content-type checks) so we run antiforgery only for
        // non-API paths. Without this split the antiforgery middleware was
        // returning 503 on internal Blazor-driven POSTs to /api/* when the
        // circuit couldn't surface a fresh token.
        app.UseWhen(
            ctx => !ctx.Request.Path.StartsWithSegments("/api"),
            branch => branch.UseAntiforgery());

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
