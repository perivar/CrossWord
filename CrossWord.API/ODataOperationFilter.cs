using Microsoft.AspNetCore.Mvc.Controllers;
using Microsoft.AspNetCore.OData.Query;
using Swashbuckle.AspNetCore.SwaggerGen;
using Microsoft.OpenApi.Models;
using Microsoft.OpenApi.Any;
using System.Text.Json;

namespace CrossWord.API
{
    /// <summary>
    /// Help your swagger show OData query options with example pre-fills
    /// </summary>
    /// <see>https://stackoverflow.com/questions/41973356/is-there-a-way-to-get-swashbuckle-to-add-odata-parameters-to-web-api-2-iqueryabl</see>
    /// <seealso>https://stackoverflow.com/questions/31351293/odata-query-in-swagger-ui</seealso>
    /// <seealso>https://www.jacobmohl.dk/how-to-add-odata-parameters-to-your-aspnet-core-api</seealso>
    public class ODataOperationFilter : IOperationFilter
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
            if (operation.Parameters == null) operation.Parameters = new List<OpenApiParameter>();

            // check for EnableQueryAttribute
            // var isQueryable = context.ApiDescription.ActionDescriptor.EndpointMetadata.Any(em => em is EnableQueryAttribute);

            var descriptor = context.ApiDescription.ActionDescriptor as ControllerActionDescriptor;
            if (descriptor != null && descriptor.FilterDescriptors.Any(filter => filter.Filter is EnableQueryAttribute))
            {
                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$select",
                    In = ParameterLocation.Query,
                    Schema = stringSchema,
                    Description = "Returns only the selected properties. (ex. FirstName, LastName, City)",
                    Required = false,
                    // Example = GetExample(typeof(string), "WordId,Value"),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$expand",
                    In = ParameterLocation.Query,
                    Schema = stringSchema,
                    Description = "Include only the selected objects. (ex. Childrens, Locations)",
                    Required = false,
                    // Example = GetExample(typeof(string), ""),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$filter",
                    In = ParameterLocation.Query,
                    Schema = stringSchema,
                    Description = "Filter the response with OData filter queries.",
                    Required = false,
                    // Example = GetExample(typeof(string), "Value eq 'ABC'"),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$top",
                    In = ParameterLocation.Query,
                    Schema = intSchema,
                    Description = "Number of objects to return. (ex. 25)",
                    Required = false,
                    // Example = GetExample(typeof(int), 50),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$skip",
                    In = ParameterLocation.Query,
                    Schema = intSchema,
                    Description = "Number of objects to skip in the current order (ex. 50)",
                    Required = false,
                    // Example = GetExample(typeof(int), 100),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$orderby",
                    In = ParameterLocation.Query,
                    Schema = stringSchema,
                    Description = "Define the order by one or more fields (ex. LastModified)",
                    Required = false,
                    // Example = GetExample(typeof(string), "WordId,Value DESC"),
                });

                operation.Parameters.Add(new OpenApiParameter()
                {
                    Name = "$count",
                    In = ParameterLocation.Query,
                    Schema = booleanSchema,
                    Description = "Return the total count.",
                    Required = false,
                    // Example = GetExample(typeof(bool), true),
                });
            }
        }
    }
}