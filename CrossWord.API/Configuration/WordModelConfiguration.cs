using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;

namespace CrossWord.API.Configuration
{
    /// <summary>
    /// Represents the model configuration for words.
    /// </summary>
    public class WordModelConfiguration : IModelConfiguration
    {
        /// <summary>
        /// Applies model configurations using the provided builder for the specified API version.
        /// </summary>
        /// <param name="builder">The <see cref="ODataModelBuilder">builder</see> used to apply configurations.</param>
        /// <param name="apiVersion">The <see cref="ApiVersion">API version</see> associated with the <paramref name="builder"/>.</param>
        public void Apply(ODataModelBuilder builder, ApiVersion apiVersion)
        {
            var word = builder.EntitySet<Word>("words").EntityType;
            word.HasKey(o => o.WordId);

            word.Filter(); // Allow for the $filter Command
            word.Count(); // Allow for the $count Command
            word.Expand(); // Allow for the $expand Command
            word.OrderBy(); // Allow for the $orderby Command
            word.Page(); // Allow for the $top and $skip Commands
            word.Select(); // Allow for the $select Command;     
        }
    }
}
