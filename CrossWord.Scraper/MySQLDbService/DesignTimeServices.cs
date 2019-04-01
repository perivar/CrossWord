using Microsoft.EntityFrameworkCore.Design;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.Extensions.DependencyInjection;

namespace CrossWord.Scraper.MySQLDbService
{
    public class DesignTimeServices : IDesignTimeServices
    {
        public void ConfigureDesignTimeServices(IServiceCollection serviceCollection)
        {
            // replace sql generators
            serviceCollection
                .AddSingleton<IMigrationsSqlGenerator, CustomMySqlMigrationsSqlGenerator>();
        }
    }
}