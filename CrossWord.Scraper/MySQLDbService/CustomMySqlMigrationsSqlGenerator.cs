using System;
using System.Linq;
using Microsoft.EntityFrameworkCore.Infrastructure;
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

        // https://github.com/PomeloFoundation/Pomelo.EntityFrameworkCore.MySql/blob/e2bf0fd6e3b3c903d75993041bf26f541f7c0885/src/EFCore.MySql/Migrations/MySqlMigrationsSqlGenerator.cs
        // https://github.com/aspnet/EntityFrameworkCore/blob/6cb41531df6dab77bc8f4ad85b08d1e99cb6d000/src/EFCore.Relational/Migrations/MigrationsSqlGenerator.cs

        // protected override void Generate(
        //     MigrationOperation operation,
        //     IModel model,
        //     MigrationCommandListBuilder builder)
        // {
        //     Log.Debug("Generating SQL for MigrationOperation");

        //     var dropDatabaseOperation = operation as MySqlDropDatabaseOperation;
        //     if (operation is MySqlCreateDatabaseOperation createDatabaseOperation)
        //     {
        //         Generate(createDatabaseOperation, model, builder);
        //     }
        //     else if (dropDatabaseOperation != null)
        //     {
        //         Generate(dropDatabaseOperation, model, builder);
        //     }
        //     else
        //     {
        //         base.Generate(operation, model, builder);
        //     }
        // }

        // protected override void Generate(
        //     CreateTableOperation operation,
        //     IModel model,
        //     MigrationCommandListBuilder builder,
        //     bool terminate)
        // {
        //     Log.Information("Generating SQL for CreateTableOperation");

        //     builder
        //         .Append("CREATE TABLE ")
        //         .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(operation.Name, operation.Schema))
        //         .AppendLine(" (");

        //     using (builder.Indent())
        //     {
        //         CreateTableColumns(operation, model, builder);
        //         // CreateTableConstraints(operation, model, builder);
        //         builder.AppendLine();
        //     }

        //     builder.Append(")");

        //     if (terminate)
        //     {
        //         builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
        //         EndStatement(builder);
        //     }
        // }

        // protected void CreateTableColumns(
        //     CreateTableOperation operation,
        //     IModel model,
        //     MigrationCommandListBuilder builder)
        // {

        //     for (var i = 0; i < operation.Columns.Count; i++)
        //     {
        //         var column = operation.Columns[i];
        //         ColumnDefinition(column, model, builder);

        //         if (i != operation.Columns.Count - 1)
        //         {
        //             builder.AppendLine(",");
        //         }
        //     }
        // }


        // test this with: dotnet ef migrations script
        // and adding debug statements to builder.append()
        protected override void ColumnDefinition(
                    string schema,
                    string table,
                    string name,
                    Type clrType,
                    string type,
                    bool? unicode,
                    int? maxLength,
                    bool? fixedLength,
                    bool rowVersion,
                    bool nullable,
                    object defaultValue,
                    string defaultValueSql,
                    string computedColumnSql,
                    bool identity,
                    IAnnotatable annotatable,
                    IModel model,
                    MigrationCommandListBuilder builder)
        {

            base.ColumnDefinition(schema, table, name, clrType, type, unicode, maxLength, fixedLength, rowVersion, nullable, defaultValue, defaultValueSql, computedColumnSql, identity, annotatable, model, builder);

            var annotation = annotatable.GetAnnotations().FirstOrDefault(a => a.Name.Equals("MySql:Collation"));
            if (annotation != null)
            {
                string collateValue = annotation.Value.ToString();

                builder.Append(string.Format(" COLLATE {0}", collateValue));
            }
        }

        // protected override void Generate(
        //     AlterColumnOperation alterColumnOperation,
        //     IModel model,
        //     MigrationCommandListBuilder builder)
        // {
        //     Log.Debug("Generating SQL for AlterColumnOperation");

        //     base.Generate(alterColumnOperation, model, builder);

        //     var annotation = alterColumnOperation.GetAnnotations().FirstOrDefault(a => a.Name.Equals("MySql:Collation"));
        //     if (annotation != null)
        //     {
        //         string collateValue = annotation.Value.ToString();

        //         Log.Information("AlterColumnOperation {0} {1} on {2}", annotation.Name, annotation.Value, alterColumnOperation.Name);

        //         // ALTER TABLE `Hints` MODIFY `Value` text CHARACTER SET utf8mb4 COLLATE utf8mb4_0900_as_cs;
        //         builder
        //                  .Append("ALTER TABLE ")
        //                  .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Table, alterColumnOperation.Schema))
        //                  .Append(" MODIFY ")
        //                  .Append(Dependencies.SqlGenerationHelper.DelimitIdentifier(alterColumnOperation.Name))
        //                  .Append(" ")
        //                  .Append(alterColumnOperation.ColumnType)
        //                  .Append(" COLLATE ")
        //                  .Append(collateValue)
        //                  .AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator)
        //                  .EndCommand(suppressTransaction: true);
        //     }
        // }
    }
}