using Azure;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using CoffeeNChill.Functions.Models;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Services;

public enum FileValidationError
{
    None,
    MissingFile,
    UnsupportedExtension,
    FileTooLarge
}

public class FileValidationResult
{
    public bool IsValid { get; init; }
    public FileValidationError Error { get; init; } = FileValidationError.None;
    public string Message { get; init; } = string.Empty;

    public static FileValidationResult Success() =>
        new() { IsValid = true, Error = FileValidationError.None, Message = string.Empty };

    public static FileValidationResult Fail(FileValidationError error, string message) =>
        new() { IsValid = false, Error = error, Message = message };
}

public class DocumentDownloadResult
{
    public bool Found { get; init; }
    public Stream? Content { get; init; }
    public long Size { get; init; }

    public static DocumentDownloadResult NotFound() => new() { Found = false };

    public static DocumentDownloadResult Success(Stream content, long size) =>
        new() { Found = true, Content = content, Size = size };
}

// Handles upload, listing, and download of staff documents via Azure Blob Storage.
// Blob Storage is used (not Azure Files) because Azurite does not emulate the File service.
public class DocumentStorageService
{
    public const long MaxFileSizeBytes = 52_428_800; // 50MB

    private static readonly HashSet<string> AllowedExtensions =
        new(StringComparer.OrdinalIgnoreCase) { ".pdf", ".txt", ".docx", ".xlsx" };

    private static readonly Dictionary<string, string> ContentTypesByExtension =
        new(StringComparer.OrdinalIgnoreCase)
        {
            [".pdf"] = "application/pdf",
            [".txt"] = "text/plain",
            [".docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
            [".xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet"
        };
    private readonly BlobContainerClient _containerClient;
    private readonly ILogger<DocumentStorageService> _logger;

    public DocumentStorageService(
        string connectionString,
        ILogger<DocumentStorageService> logger,
        string containerName = "staff-docs")
    {
        _logger = logger;
        _containerClient = new BlobContainerClient(connectionString, containerName);
        _containerClient.CreateIfNotExists();
    }

    public static string GetContentType(string fileName)
    {
        var extension = Path.GetExtension(fileName);
        return ContentTypesByExtension.TryGetValue(extension, out var contentType)
            ? contentType
            : "application/octet-stream";
    }

    public FileValidationResult ValidateFile(string fileName, long size)
    {
        var extension = string.IsNullOrWhiteSpace(fileName) ? string.Empty : Path.GetExtension(fileName);

        if (string.IsNullOrWhiteSpace(fileName) || string.IsNullOrEmpty(extension))
        {
            return FileValidationResult.Fail(
                FileValidationError.MissingFile,
                "A file name with a valid extension is required.");
        }

        if (!AllowedExtensions.Contains(extension))
        {
            return FileValidationResult.Fail(
                FileValidationError.UnsupportedExtension,
                $"File extension '{extension}' is not supported. Allowed extensions: .pdf, .txt, .docx, .xlsx");
        }

        if (size > MaxFileSizeBytes)
        {
            return FileValidationResult.Fail(
                FileValidationError.FileTooLarge,
                $"File size {size} bytes exceeds the maximum allowed size of {MaxFileSizeBytes} bytes (50MB).");
        }

        return FileValidationResult.Success();
    }

    public async Task<UploadDocumentResponse> UploadFileAsync(string fileName, Stream fileStream, long size, string contentType)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        var options = new BlobUploadOptions
        {
            HttpHeaders = new BlobHttpHeaders { ContentType = contentType }
        };

        try
        {
            await blobClient.UploadAsync(fileStream, options);
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to upload document '{FileName}' ({Size} bytes) to blob storage",
                fileName,
                size);

            throw;
        }

        _logger.LogInformation(
            "Uploaded document '{FileName}' ({Size} bytes, {ContentType})",
            fileName,
            size,
            contentType);

        return new UploadDocumentResponse
        {
            FileName = fileName,
            Size = size,
            ContentType = contentType,
            UploadedAt = DateTimeOffset.UtcNow
        };
    }

    public async Task<DocumentListItem[]> ListFilesAsync()
    {
        var items = new List<DocumentListItem>();

        try
        {
            await foreach (BlobItem item in _containerClient.GetBlobsAsync())
            {
                items.Add(new DocumentListItem
                {
                    FileName = item.Name,
                    Size = item.Properties.ContentLength ?? 0,
                    LastModified = item.Properties.LastModified
                });
            }
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to list documents from blob storage");

            throw;
        }

        _logger.LogInformation(
            "Listed {Count} documents",
            items.Count);

        return items.ToArray();
    }

    public async Task<DocumentDownloadResult> DownloadFileAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);

        if (!await blobClient.ExistsAsync())
        {
            _logger.LogWarning(
                "Requested document '{FileName}' does not exist",
                fileName);

            return DocumentDownloadResult.NotFound();
        }

        try
        {
            BlobDownloadInfo download = await blobClient.DownloadAsync();

            _logger.LogInformation(
                "Downloaded document '{FileName}' ({Size} bytes)",
                fileName,
                download.ContentLength);

            return DocumentDownloadResult.Success(
                download.Content,
                download.ContentLength);
        }
        catch (RequestFailedException ex) when (ex.Status == 404)
        {
            _logger.LogWarning(
                "Document '{FileName}' returned 404 on download despite passing the existence check",
                fileName);

            return DocumentDownloadResult.NotFound();
        }
        catch (RequestFailedException ex)
        {
            _logger.LogError(
                ex,
                "Failed to download document '{FileName}'",
                fileName);

            throw;
        }
    }
}
