namespace MotorPortal.API.Models;

/// <summary>
/// Wrapper for the multipart/form-data batch upload request. Binding all form fields through a single
/// complex [FromForm] parameter (rather than separate [FromForm] scalar + IFormFile parameters) is the
/// pattern Swashbuckle can actually generate an OpenAPI operation for - see FileUploadOperationFilter.
/// The wire format (field names productId/functionId/file) is unchanged.
/// </summary>
public class BatchUploadRequest
{
    public int ProductId { get; set; }
    public int FunctionId { get; set; }
    public IFormFile File { get; set; } = null!;
}
