using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.AspNet.OData.Builder;
using Microsoft.AspNetCore.Mvc;
using Microsoft.OData.Edm;

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
            var word = builder.EntitySet<Word>("Words").EntityType;
            word.HasKey(o => o.WordId);

            // bind a function to the words odata controller
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$select=WordId,Value&$top=20&orderby=WordId%20desc
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$apply=groupby((Value))&$top=100&$count=true
            builder
                .EntityType<Word>().Collection // bound to Word, comment to make it unbounded
                .Function("Synonyms")
                // .ReturnsCollection<Word>() // use when unbounded
                .ReturnsCollectionFromEntitySet<Word>("Words") // Bound to the Words odata controller
                .Parameter<string>("Word");

            // builder
            //     .EntityType<Word>().Collection
            //     .Action("Synonyms")
            //     .ReturnsCollectionFromEntitySet<Word>("Words") // Bound to the Words odata controller
            //     .Parameter<string>("Word");

            // var word = builder.EntitySet<Word>("Words").EntityType;
            // word.HasKey(o => o.WordId);
            // word.Filter(); // Allow for the $filter Command
            // word.Count(); // Allow for the $count Command
            // word.Expand(); // Allow for the $expand Command
            // word.OrderBy(); // Allow for the $orderby Command
            // word.Page(); // Allow for the $top and $skip Commands
            // word.Select(); // Allow for the $select Command;     
        }

        public static IEdmModel GetEdmModel(ODataModelBuilder builder)
        {
            builder.EntitySet<Word>("Words");

            // bind a function to the words odata controller
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$select=WordId,Value&$top=20&orderby=WordId%20desc
            // GET /odata/Words/Synonyms(Word='FORFATTER')?$apply=groupby((Value))&$top=100&$count=true
            builder
                .EntityType<Word>().Collection // bound to Word, comment to make it unbounded
                .Function("Synonyms")
                // .ReturnsCollection<Word>() // use when unbounded
                .ReturnsCollectionFromEntitySet<Word>("Words") // Bound to the Words odata controller
                .Parameter<string>("Word");

            return builder.GetEdmModel();
        }
    }
}
