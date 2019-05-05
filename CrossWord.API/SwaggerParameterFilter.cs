using Microsoft.AspNetCore.Mvc.Abstractions;
using Microsoft.AspNetCore.Mvc.ApiExplorer;
using Swashbuckle.AspNetCore.Swagger;
using Swashbuckle.AspNetCore.SwaggerGen;
using System;
using System.Collections.Generic;
using System.Linq;
using static Microsoft.AspNetCore.Mvc.Versioning.ApiVersionMapping;

namespace CrossWord.API
{
    /// <summary>
    /// Represents the Swagger/Swashbuckle parameter filter
    /// </summary>
    public class SwaggerParameterFilter : IParameterFilter
    {
        public void Apply(IParameter parameter, ParameterFilterContext context)
        {
            // var type = context.ParameterInfo?.ParameterType;
            // if (type == null)
            //     return;

            // if (type.IsEnum)
            // {
            //     var names = Enum.GetNames(type);
            //     var values = Enum.GetValues(type);
            //     var desc = "";

            //     foreach (var value in values)
            //     {
            //         var intValue = Convert.ChangeType(value, Enum.GetUnderlyingType(value.GetType()));
            //         desc += $"{intValue}={value},";
            //     }
            // 
            //     desc = desc.TrimEnd(',');
            //     if (!parameter.Extensions.ContainsKey("x-enumNames"))
            //         parameter.Extensions.Add("x-enumNames", names);
            // }
        }
    }
}