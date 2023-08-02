using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData.Edm;

namespace CrossWord.API.Configuration
{
    /// <summary>
    /// Represents the model configuration for words.
    /// This is injected automatically in Startup Configure using VersionedODataModelBuilder modelBuilder
    /// </summary>
    public class WordModelConfiguration : IModelConfiguration
    {
        /// <summary>
        /// Applies model configurations using the provided builder for the specified API version.
        /// </summary>
        /// <param name="builder">The <see cref="ODataModelBuilder">builder</see> used to apply configurations.</param>
        /// <param name="apiVersion">The <see cref="ApiVersion">API version</see> associated with the <paramref name="builder"/>.</param>
        public void Apply(ODataModelBuilder builder, ApiVersion apiVersion, string? routePrefix)
        {
            builder.EntitySet<Word>("Words").EntityType.HasKey(o => o.WordId);

            BindFunctions(builder);
        }

        public static IEdmModel GetEdmModel(ODataModelBuilder builder)
        {
            builder.EntitySet<Word>("Words").EntityType.HasKey(o => o.WordId);

            BindFunctions(builder);

            return builder.GetEdmModel();
        }

        public static void BindFunctions(ODataModelBuilder builder)
        {
            // bind a function to the words odata controller
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$select=WordId,Value&$top=20&orderby=WordId%20desc
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$apply=groupby((Value))&$top=100&$count=true
            var function = builder
                .EntityType<Word>().Collection // bound to Word, comment to make it unbounded
                .Function("Synonyms")
                // .ReturnsCollection<Word>() // use when unbounded
                .ReturnsCollectionFromEntitySet<Word>("Words") // Bound to the Words odata controller                
                ;
            function.Parameter<string>("Word");

            // bind a function to the words odata controller
            // GET /odata/Words/Synonyms(Word='FORFATTER', Pattern='_____')?$select=WordId,Value&$top=20&orderby=WordId%20desc
            // GET /odata/Words/Synonyms(Word='FORFATTER', Pattern='_____')?$apply=groupby((Value))&$top=100&$count=true
            var functionWithPattern = builder
                .EntityType<Word>().Collection // bound to Word, comment to make it unbounded
                .Function("Synonyms")
                // .ReturnsCollection<Word>() // use when unbounded
                .ReturnsCollectionFromEntitySet<Word>("Words") // Bound to the Words odata controller
                ;
            functionWithPattern.Parameter<string>("Word");
            functionWithPattern.Parameter<string>("Pattern");
        }
    }
}
