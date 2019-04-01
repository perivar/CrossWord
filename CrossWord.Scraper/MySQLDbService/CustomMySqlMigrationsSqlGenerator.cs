using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;

namespace CrossWord.Scraper.MySQLDbService
{
    public class CustomMySqlMigrationsSqlGenerator : MySqlMigrationsSqlGenerator
    {
        public CustomMySqlMigrationsSqlGenerator(
                    MigrationsSqlGeneratorDependencies dependencies,
                    IMigrationsAnnotationProvider migrationsAnnotations,
                    IMySqlOptions options)
                    : base(dependencies, migrationsAnnotations, options)
        {
        }

        protected override void Generate(
            AlterColumnOperation alterColumnOperation,
            IModel model,
            MigrationCommandListBuilder builder)
        {

            base.Generate(alterColumnOperation, model, builder);

            var annotation = alterColumnOperation.GetAnnotations().FirstOrDefault(a => a.Name.Equals("MySQL:Collation"));
            if (annotation != null)
            {
                string collateValue = annotation.Value.ToString();

                // ALTER TABLE `Hints` CHANGE `Value` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs;
                builder
                         .Append("ALTER TABLE ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Table, alterColumnOperation.Schema))
                         .Append(" CHANGE ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Name))
                         .Append(" text CHARACTER SET utf8mb4 COLLATE ")
                         .Append(collateValue)
                         .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                         .EndCommand(suppressTransaction: true);
            }
        }
    }
}