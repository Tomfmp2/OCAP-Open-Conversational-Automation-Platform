using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using OCAP.Core.Storage;
using OCAP.Providers.AzureBlob;
using OCAP.Providers.GoogleDrive;
using OCAP.Providers.LocalStorage;
using OCAP.Providers.Microsoft365;
using OCAP.Providers.S3;

namespace OCAP.Infrastructure.Extensions;

public static class ObjectStorageServiceExtensions
{
    public static IServiceCollection AddObjectStorage(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        var section = configuration.GetSection(StorageOptions.SectionName);
        var options = section.Get<StorageOptions>() ?? new StorageOptions();
        services.Configure<StorageOptions>(section);
        services.AddSingleton(options);

        services.AddHttpClient("OCAP.GoogleDrive");
        services.AddHttpClient("OCAP.OneDrive");

        services.AddSingleton<IObjectStorage>(serviceProvider =>
            options.Provider.Trim().ToUpperInvariant() switch
            {
                "" or "LOCAL" or "LOCALSTORAGE" => new LocalObjectStorage(options),
                "S3" or "MINIO" => new S3ObjectStorage(options),
                "AZURE" or "AZUREBLOB" or "AZUREBLOBSTORAGE" => new AzureBlobObjectStorage(options),
                "GOOGLEDRIVE" or "GDRIVE" => new GoogleDriveObjectStorage(
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("OCAP.GoogleDrive"),
                    options),
                "ONEDRIVE" or "MICROSOFT365" => new OneDriveObjectStorage(
                    serviceProvider.GetRequiredService<IHttpClientFactory>().CreateClient("OCAP.OneDrive"),
                    options),
                _ => throw new NotSupportedException(
                    $"Proveedor de almacenamiento no soportado: '{options.Provider}'.")
            });

        return services;
    }
}
