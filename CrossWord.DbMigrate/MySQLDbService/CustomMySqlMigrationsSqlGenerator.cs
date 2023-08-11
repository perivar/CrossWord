using Microsoft.EntityFrameworkCore.Metadata;
using Microsoft.EntityFrameworkCore.Migrations;
using Microsoft.EntityFrameworkCore.Migrations.Operations;
using Microsoft.EntityFrameworkCore.Update;
using Pomelo.EntityFrameworkCore.MySql.Infrastructure.Internal;
using Pomelo.EntityFrameworkCore.MySql.Migrations;
using System.Linq;
using Serilog;

namespace CrossWord.DbMigrate.MySQLDbService
{
    public class CustomMySqlMigrationsSqlGenerator : MySqlMigrationsSqlGenerator
    {
        private ILogger logger;

        private void InitLoggerIfNull()
        {
            if (logger == null)
            {
                logger = Log.ForContext<CustomMySqlMigrationsSqlGenerator>();
            }
        }

        public CustomMySqlMigrationsSqlGenerator(
                    MigrationsSqlGeneratorDependencies dependencies,
                    ICommandBatchPreparer commandBatchPreparer,
                    IMySqlOptions options
                    )
                    : base(dependencies, commandBatchPreparer, options)
        {
            InitLoggerIfNull();
        }

        // test this with: dotnet ef migrations script
        // and adding debug statements to builder.append()
        protected override void ColumnDefinition(
            string schema,
            string table,
            string name,
            ColumnOperation operation,
            IModel model,
            MigrationCommandListBuilder builder)
        {
            if (logger != null) logger.Verbose("ColumnDefinition for {0}: {1} [{2}]", table, name, string.Join(";", operation.GetAnnotations().Select(x => x.Name)));

            base.ColumnDefinition(schema, table, name, operation, model, builder);

            // use the old "MySql:Collation" since this is what is used in WordHintDbContext
            var annotationName = "MySql:Collation";
            var annotation = operation.FindAnnotation(annotationName);
            if (annotation != null)
            {
                string collateValue = annotation.Value.ToString();
                if (logger != null) logger.Debug("Found {0} for {1}:{2}, fixing COLLATE: {3}", annotationName, table, name, collateValue);

                builder.Append(string.Format(" COLLATE {0}", collateValue));
            }
        }

        protected override void Generate(
            AlterColumnOperation alterColumnOperation,
            IModel model,
            MigrationCommandListBuilder builder)
        {
            builder
                .Append("ALTER TABLE ")
                .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Table, alterColumnOperation.Schema))
                .Append(" MODIFY COLUMN ");

            // TODO: The IsUnique on Word Value screws up the sql generation and removes the COLLATE section
            // I.e from 
            // ALTER TABLE `Words` MODIFY COLUMN `Value` longtext NULL COLLATE utf8mb4_0900_as_cs
            // to
            // ALTER TABLE `Words` MODIFY COLUMN `Value` varchar(255) NULL;
            // SO HARDCODE A FIX!
            if (alterColumnOperation.Table == "Words" && alterColumnOperation.Name == "Value")
            {
                if (logger != null) logger.Debug("Found AlterColumn for {0}:{1}, fixing COLLATE utf8mb4_0900_as_cs", alterColumnOperation.Table, alterColumnOperation.Name);

                builder
                        .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Name))
                        .Append(" varchar(255) NULL COLLATE utf8mb4_0900_as_cs")
                        .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
                        .EndCommand(suppressTransaction: true);
            }
            else
            {
                // Normal handling
                ColumnDefinition(
                    alterColumnOperation.Schema,
                    alterColumnOperation.Table,
                    alterColumnOperation.Name,
                    alterColumnOperation,
                    model,
                    builder);

                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                builder.EndCommand();
            }
        }
    }
}