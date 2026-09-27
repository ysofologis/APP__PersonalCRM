using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using PersonalCrm.Infrastructure.Persistence;

namespace PersonalCrm.Migrations;

public static class Program
{
    /// <summary>
    /// CLI entry point for running EF Core migrations against the configured
    /// database without booting the whole Blazor app.
    ///
    /// Usage:
    ///   dotnet run --project src/PersonalCrm.Migrations -- update
    ///   dotnet run --project src/PersonalCrm.Migrations -- list
    ///   dotnet run --project src/PersonalCrm.Migrations -- script migrations.sql
    ///
    /// Environment variables are read from <c>appsettings.json</c> in this
    /// project plus the standard ASP.NET Core env vars (e.g.
    /// <c>ConnectionStrings__Default</c>).
    /// </summary>
    public static async Task<int> Main(string[] args)
    {
        var config = new ConfigurationBuilder()
            .SetBasePath(AppContext.BaseDirectory)
            .AddJsonFile("appsettings.json", optional: true)
            .AddEnvironmentVariables()
            .Build();

        var connStr = config.GetConnectionString("Default")
                      ?? Environment.GetEnvironmentVariable("ConnectionStrings__Default")
                      ?? "Data Source=./data/personal-crm.db";

        var host = Host.CreateDefaultBuilder()
            .ConfigureServices(services =>
            {
                services.AddDbContext<AppDbContext>(opt =>
                    opt.UseSqlite(connStr, sqlite =>
                    {
                        sqlite.MigrationsAssembly(typeof(AppDbContext).Assembly.GetName().Name);
                        sqlite.MigrationsHistoryTable("__EFMigrationsHistory");
                    })
                       .EnableSensitiveDataLogging(false));
            })
            .ConfigureLogging(lb => lb.AddSimpleConsole().SetMinimumLevel(LogLevel.Warning))
            .Build();

        await using var scope  = host.Services.CreateAsyncScope();
        var db                 = scope.ServiceProvider.GetRequiredService<AppDbContext>();

        var command = args.Length > 0 ? args[0].ToLowerInvariant() : "update";

        try
        {
            return command switch
            {
                "update"  => await UpdateAsync(db),
                "list"    => ListAsync(db),
                "script"  => await ScriptAsync(db, args),
                "drop"    => await DropAsync(db),
                "help" or "--help" or "-h" => PrintHelp(),
                _         => Fail($"Unknown command '{command}'. Run with 'help' for usage.")
            };
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine($"error: {ex.Message}");
            return 1;
        }
    }

    private static async Task<int> UpdateAsync(AppDbContext db)
    {
        var pending = await db.Database.GetPendingMigrationsAsync();
        if (!pending.Any())
        {
            Console.WriteLine("Database is up to date. No pending migrations.");
            return 0;
        }
        Console.WriteLine($"Applying {pending.Count()} migration(s):");
        foreach (var m in pending) Console.WriteLine($"  - {m}");
        await db.Database.MigrateAsync();
        Console.WriteLine("Done.");
        return 0;
    }

    private static int ListAsync(AppDbContext db)
    {
        // EF Core 10 dropped the *Async variants of the migration-listing
        // helpers — use the synchronous ones from RelationalDatabaseFacadeExtensions.
        var applied    = db.Database.GetAppliedMigrations().ToList();
        var migrations = db.Database.GetMigrations().ToList();
        Console.WriteLine("Migrations:");
        foreach (var m in migrations)
        {
            var marker = applied.Contains(m) ? "✓" : "·";
            Console.WriteLine($"  [{marker}] {m}");
        }
        return 0;
    }

    private static async Task<int> ScriptAsync(AppDbContext db, string[] args)
    {
        var output = args.Length > 1 ? args[1] : "migrations.sql";
        // EF Core 10: GenerateCreateScript is sync only.
        var script = db.Database.GenerateCreateScript();
        await File.WriteAllTextAsync(output, script);
        Console.WriteLine($"Wrote {output} ({script.Length:N0} chars).");
        return 0;
    }

    private static async Task<int> DropAsync(AppDbContext db)
    {
        Console.Write("This will DELETE all data. Type 'yes' to continue: ");
        var confirm = Console.ReadLine();
        if (!string.Equals(confirm, "yes", StringComparison.Ordinal))
        {
            Console.WriteLine("Aborted.");
            return 0;
        }
        await db.Database.EnsureDeletedAsync();
        Console.WriteLine("Database deleted.");
        return 0;
    }

    private static int PrintHelp()
    {
        Console.WriteLine("""
            Personal CRM migrations CLI

            Usage:
              dotnet run --project src/PersonalCrm.Migrations -- <command> [args]

            Commands:
              update                Apply all pending migrations (default).
              list                  Show all migrations and their applied status.
              script [output.sql]   Write the create script to a file (default: migrations.sql).
              drop                  Delete the database (requires interactive confirmation).
              help                  Show this message.

            Configuration:
              ConnectionStrings:Default   SQLite connection string. Falls back to
                                        ./data/personal-crm.db. Honoured from
                                        appsettings.json, environment variables
                                        (ConnectionStrings__Default), or command line.
            """);
        return 0;
    }

    private static int Fail(string message)
    {
        Console.Error.WriteLine(message);
        return 2;
    }
}
