namespace CrossWord.API;

using System.Text.Json;
using Microsoft.AspNet.OData;
using Microsoft.OpenApi.Any;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

/// <summary>
/// Help your swagger show OData query options with example pre-fills
/// </summary>
/// <see>https://stackoverflow.com/questions/41973356/is-there-a-way-to-get-swashbuckle-to-add-odata-parameters-to-web-api-2-iqueryabl</see>
/// <seealso>https://stackoverflow.com/questions/31351293/odata-query-in-swagger-ui</seealso>
/// <seealso>https://www.jacobmohl.dk/how-to-add-odata-parameters-to-your-aspnet-core-api</seealso>
public class SwaggerEnableQueryFilter : IOperationFilter
{
    private static readonly OpenApiSchema stringSchema = new() { Type = "string" };
    private static readonly OpenApiSchema intSchema = new() { Type = "integer" };
    private static readonly OpenApiSchema booleanSchema = new() { Type = "boolean" };

    private static IOpenApiAny GetExample(Type type, object value)
    {
        // REF: https://github.com/Microsoft/aspnet-api-versioning/issues/429#issuecomment-605402330
        var json = JsonSerializer.Serialize(value, type);
        return OpenApiAnyFactory.CreateFromJson(json);
    }

    public void Apply(OpenApiOperation operation, OperationFilterContext context)
    {
        var isQueryable = context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(em => em is EnableQueryAttribute);

        if (isQueryable)
        {
            var newODataParameters = new List<OpenApiParameter>
                {
                    // Note! disabling Examples since removing them manually when testing in Swagger is to much hassle
                    new OpenApiParameter
                    {
                        Name = "$filter",
                        Description = "Filter the results using OData syntax.",
                        // Example = GetExample(typeof(string), "Value eq 'ABC'"),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = stringSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$select",
                        Description = "Trim the fields returned using OData syntax",
                        // Example = GetExample(typeof(string), "WordId,Value"),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = stringSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$orderby",
                        Description = "Order the results using OData syntax.",
                        // Example = GetExample(typeof(string), "WordId,Value DESC"),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = stringSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$skip",
                        Description = "The number of records to skip.",
                        // Example = GetExample(typeof(int), 100),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = intSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$top",
                        Description = "The max number of records.",
                        // Example = GetExample(typeof(int), 50),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = intSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$expand",
                        Description = "Expands related entities inline.",
                        // Example = GetExample(typeof(string), ""),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = stringSchema
                    },
                    new OpenApiParameter
                    {
                        Name = "$count",
                        Description = "Return the total count.",
                        // Example = GetExample(typeof(bool), true),
                        Required = false,
                        In = ParameterLocation.Query,
                        Schema = booleanSchema
                    }
                };

            operation.Parameters ??= new List<OpenApiParameter>();

            foreach (var item in newODataParameters)
            {
                operation.Parameters.Add(item);
            }
        }
    }
}
