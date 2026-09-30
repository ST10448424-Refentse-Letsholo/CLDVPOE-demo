namespace CoffeeNChill.Functions.Models;

// Returned after a successful upload to the staff-docs file share.
public class UploadDocumentResponse
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public string ContentType { get; set; } = string.Empty;
    public DateTimeOffset UploadedAt { get; set; }
}

// One entry in the ListDocuments response.
public class DocumentListItem
{
    public string FileName { get; set; } = string.Empty;
    public long Size { get; set; }
    public DateTimeOffset? LastModified { get; set; }
}
