using Amazon.S3;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace PersonalCrm.Infrastructure.Storage;

/// <summary>
/// DI registration for attachment stores. Selects <see cref="LocalAttachmentStore"/>
/// or <see cref="S3AttachmentStore"/> based on the bound
/// <see cref="AttachmentStoreOptions.Provider"/>.
/// </summary>
public static class AttachmentStoreExtensions
{
    public static IServiceCollection AddAttachmentStore(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        ArgumentNullException.ThrowIfNull(services);
        ArgumentNullException.ThrowIfNull(configuration);

        services.Configure<AttachmentStoreOptions>(
            configuration.GetSection(AttachmentStoreOptions.SectionName));

        // Build the IAmazonS3 client lazily so apps that don't use S3 don't
        // pay for SDK initialisation.
        services.AddSingleton<IAmazonS3>(sp =>
        {
            var opts = configuration
                .GetSection(AttachmentStoreOptions.SectionName)
                .GetSection("S3")
                .Get<AttachmentStoreOptions.S3Options>()
                ?? new AttachmentStoreOptions.S3Options();

            var config = new AmazonS3Config
            {
                RegionEndpoint          = Amazon.RegionEndpoint.GetBySystemName(opts.Region),
                ForcePathStyle          = opts.ForcePathStyle,
                ServiceURL              = string.IsNullOrWhiteSpace(opts.Endpoint) ? null : opts.Endpoint,
                UseHttp                 = opts.Endpoint?.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ?? false
            };

            return new AmazonS3Client(
                string.IsNullOrWhiteSpace(opts.AccessKey) ? null : opts.AccessKey,
                string.IsNullOrWhiteSpace(opts.SecretKey) ? null : opts.SecretKey,
                config);
        });

        // Provider selection. Done at registration time based on the bound
        // configuration so the rest of the app can depend on IAttachmentStore.
        var provider = configuration
            .GetSection(AttachmentStoreOptions.SectionName)
            .GetValue<string>("Provider")
            ?? "local";

        services.AddSingleton<IAttachmentStore, LocalAttachmentStore>(_ => new LocalAttachmentStore(
            Microsoft.Extensions.Options.Options.Create(
                configuration.GetSection(AttachmentStoreOptions.SectionName)
                    .Get<AttachmentStoreOptions>() ?? new AttachmentStoreOptions())));

        if (string.Equals(provider, "s3", StringComparison.OrdinalIgnoreCase))
        {
            services.AddSingleton<IAttachmentStore, S3AttachmentStore>(sp =>
                new S3AttachmentStore(
                    sp.GetRequiredService<IAmazonS3>(),
                    Microsoft.Extensions.Options.Options.Create(
                        configuration.GetSection(AttachmentStoreOptions.SectionName)
                            .Get<AttachmentStoreOptions>() ?? new AttachmentStoreOptions())));
        }

        return services;
    }
}
