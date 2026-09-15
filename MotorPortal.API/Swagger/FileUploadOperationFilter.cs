using Microsoft.AspNetCore.Http;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace MotorPortal.API.Swagger;

/// <summary>
/// Swashbuckle cannot generate multipart/form-data operations out of the box for actions that combine
/// [FromForm] IFormFile with other [FromForm] scalar parameters (e.g. BatchesController.Upload,
/// PoliciesController.CancelUpload) - it throws SwaggerGeneratorException and takes down the whole
/// swagger.json document. This filter replaces the auto-generated parameter list with an explicit
/// multipart/form-data request body for any action with an IFormFile parameter.
/// </summary>
public class FileUploadOperationFilter : IOperationFilter
{
    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var fileParameters = context.ApiDescription.ParameterDescriptions
            .Where(p => p.ModelMetadata?.ModelType == typeof(IFormFile))
            .ToList();

        if (fileParameters.Count == 0)
        {
            return;
        }

        var properties = new Dictionary<string, OpenApiSchema>();
        var required = new HashSet<string>();

        foreach (var param in context.ApiDescription.ParameterDescriptions)
        {
            var isFile = param.ModelMetadata?.ModelType == typeof(IFormFile);
            properties[param.Name] = isFile
                ? new OpenApiSchema { Type = "string", Format = "binary" }
                : new OpenApiSchema { Type = "string" };

            if (param.IsRequired)
            {
                required.Add(param.Name);
            }
        }

        operation.RequestBody = new OpenApiRequestBody
        {
            Content =
            {
                ["multipart/form-data"] = new OpenApiMediaType
                {
                    Schema = new OpenApiSchema
                    {
                        Type = "object",
                        Properties = properties,
                        Required = required
                    }
                }
            }
        };

        // Remove the auto-generated (and unsupported) parameter entries for the fields we just
        // folded into the multipart request body.
        var namesToRemove = properties.Keys.ToHashSet();
        for (var i = operation.Parameters.Count - 1; i >= 0; i--)
        {
            if (namesToRemove.Contains(operation.Parameters[i].Name))
            {
                operation.Parameters.RemoveAt(i);
            }
        }
    }
}
