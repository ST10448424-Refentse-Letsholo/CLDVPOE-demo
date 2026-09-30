using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using CoffeeNChill.Functions.Services;
using Microsoft.Extensions.Logging;

namespace CoffeeNChill.Functions.Functions;

public class DocumentFunctions
{
    private readonly DocumentStorageService _fileShareService;
    private readonly ILogger<DocumentFunctions> _logger;

    public DocumentFunctions(
        DocumentStorageService fileShareService,
        ILogger<DocumentFunctions> logger)
    {
        _fileShareService = fileShareService;
        _logger = logger;
    }

    [Function("UploadStaffDocument")]
    public async Task<HttpResponseData> UploadStaffDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "post", Route = "documents/upload")] HttpRequestData req)
    {
        var httpContext = req.FunctionContext.GetHttpContext();
        if (httpContext is null || !httpContext.Request.HasFormContentType)
        {
            var badContentType = req.CreateResponse(HttpStatusCode.BadRequest);
            await badContentType.WriteStringAsync("Request must be multipart/form-data with a 'file' field.");
            return badContentType;
        }

        var form = await httpContext.Request.ReadFormAsync();
        var file = form.Files["file"];

        if (file is null || file.Length == 0)
        {
            var noFile = req.CreateResponse(HttpStatusCode.BadRequest);
            await noFile.WriteStringAsync("No file provided. Include a 'file' field in the multipart/form-data request.");
            return noFile;
        }

        var fileName = Path.GetFileName(file.FileName);
        var validation = _fileShareService.ValidateFile(fileName, file.Length);
        if (!validation.IsValid)
        {
            var statusCode = validation.Error == FileValidationError.UnsupportedExtension
                ? HttpStatusCode.UnsupportedMediaType
                : HttpStatusCode.BadRequest;

            var invalid = req.CreateResponse(statusCode);
            await invalid.WriteStringAsync(validation.Message);
            return invalid;
        }

        var contentType = string.IsNullOrWhiteSpace(file.ContentType)
            ? DocumentStorageService.GetContentType(fileName)
            : file.ContentType;

        try
        {
            await using var stream = file.OpenReadStream();
            var uploaded = await _fileShareService.UploadFileAsync(
                fileName,
                stream,
                file.Length,
                contentType);

            var response = req.CreateResponse(HttpStatusCode.Created);
            await response.WriteAsJsonAsync(uploaded);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error uploading document '{FileName}'",
                fileName);

            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync(
                "An error occurred while uploading the document.");

            return error;
        }
    }

    [Function("ListDocuments")]
    public async Task<HttpResponseData> ListDocuments(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents")] HttpRequestData req)
    {
        try
        {
            var items = await _fileShareService.ListFilesAsync();

            var response = req.CreateResponse(HttpStatusCode.OK);
            await response.WriteAsJsonAsync(items);
            return response;
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error listing documents");

            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync(
                "An error occurred while listing documents.");

            return error;
        }
    }

    [Function("DownloadDocument")]
    public async Task<HttpResponseData> DownloadDocument(
        [HttpTrigger(AuthorizationLevel.Anonymous, "get", Route = "documents/download/{fileName}")] HttpRequestData req, string fileName)
    {
        DocumentDownloadResult result;

        try
        {
            result = await _fileShareService.DownloadFileAsync(fileName);
        }
        catch (Exception ex)
        {
            _logger.LogError(
                ex,
                "Unhandled error downloading document '{FileName}'",
                fileName);

            var error = req.CreateResponse(HttpStatusCode.InternalServerError);
            await error.WriteStringAsync(
                "An error occurred while downloading the document.");

            return error;
        }

        if (!result.Found || result.Content is null)
        {
            var notFound = req.CreateResponse(HttpStatusCode.NotFound);
            await notFound.WriteStringAsync($"Document '{fileName}' was not found.");
            return notFound;
        }

        var response = req.CreateResponse(HttpStatusCode.OK);
        response.Headers.Add("Content-Type", DocumentStorageService.GetContentType(fileName));
        response.Headers.Add("Content-Disposition", $"attachment; filename=\"{fileName}\"");

        await using (result.Content)
        {
            await result.Content.CopyToAsync(response.Body);
        }

        return response;
    }
}
