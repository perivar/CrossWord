using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.AspNetCore.Mvc.Versioning.ApiVersionMapping;

namespace CrossWord.API
{
    /// <summary>
    /// Represents the Swagger/Swashbuckle document filter
    /// </summary>
    public class SwaggerDocumentFilter : IDocumentFilter
    {
        /// <summary>
        /// Applies the document filter to the specified operation using the given context.
        /// </summary>
        /// <param name="swaggerDoc">The document to apply the filter to.</param>
        /// <param name="context">The current operation filter context.</param>
        public void Apply(SwaggerDocument swaggerDoc, DocumentFilterContext context)
        {
            // foreach (var swaggerDocPath in swaggerDoc.Paths)
            // {
            //     // process each of the odata 
            //     if (swaggerDocPath.Key.StartsWith("/odata/"))
            //     {
            //         // RelativePath: "odata/Words"
            //         var swaggerRelativePath = swaggerDocPath.Key.Substring(1, swaggerDocPath.Key.Length - 1);
            //         var description = context.ApiDescriptions.First(p => p.RelativePath == swaggerRelativePath);

            //     }
            // }
        }
    }
}