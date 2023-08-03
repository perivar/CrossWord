using CrossWord.Scraper.MySQLDbService.Models;
using Microsoft.OData.Edm;
using Microsoft.OData.ModelBuilder;

namespace CrossWord.API.Configuration
{
    /// <summary>
    /// Represents the model configuration for words.
    /// </summary>
    public class WordModelConfiguration
    {
        public static IEdmModel GetEdmModel(ODataConventionModelBuilder builder)
        {
            builder.EnableLowerCamelCase(); // turn on "lower camel case" for the whole model 
            
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
