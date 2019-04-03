using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Serilog;
using Serilog.Events;

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
                    AddColumnOperation addColumnOperation,
                    IModel model,
                    MigrationCommandListBuilder builder)
        {
            Log.Debug("Generating SQL for AddColumnOperation");

            base.Generate(addColumnOperation, model, builder);

            var annotation = addColumnOperation.GetAnnotations().FirstOrDefault(a => a.Name.Equals("MySQL:Collation"));
            if (annotation != null)
            {
                string collateValue = annotation.Value.ToString();

                Log.Debug("AlterColumnOperation {0} {1} on {2}", annotation.Name, annotation.Value, addColumnOperation.Name);

                // ALTER TABLE `Hints` ADD `Value` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs;
                builder
                         .Append("ALTER TABLE ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(addColumnOperation.Table, addColumnOperation.Schema))
                         .Append(" ADD ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(addColumnOperation.Name))
                         .Append(" ")
                         .Append(addColumnOperation.ColumnType)
                         .Append(" COLLATE ")
                         .Append(collateValue)
                         .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                         .EndCommand(suppressTransaction: true);
            }
        }

        protected override void Generate(
            AlterColumnOperation alterColumnOperation,
            IModel model,
            MigrationCommandListBuilder builder)
        {
            Log.Debug("Generating SQL for AlterColumnOperation");

            base.Generate(alterColumnOperation, model, builder);

            var annotation = alterColumnOperation.GetAnnotations().FirstOrDefault(a => a.Name.Equals("MySQL:Collation"));
            if (annotation != null)
            {
                string collateValue = annotation.Value.ToString();

                Log.Debug("AlterColumnOperation {0} {1} on {2}", annotation.Name, annotation.Value, alterColumnOperation.Name);

                // ALTER TABLE `Hints` MODIFY `Value` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs;
                builder
                         .Append("ALTER TABLE ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Table, alterColumnOperation.Schema))
                         .Append(" MODIFY ")
                         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Name))
                         .Append(" ")
                         .Append(alterColumnOperation.ColumnType)
                         .Append(" COLLATE ")
                         .Append(collateValue)
                         .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                         .EndCommand(suppressTransaction: true);
            }
        }
    }
}