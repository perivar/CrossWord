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
                    alterColumnOperation.ClrType,
                    alterColumnOperation.ColumnType,
                    alterColumnOperation.IsUnicode,
                    alterColumnOperation.MaxLength,
                    alterColumnOperation.IsFixedLength,
                    alterColumnOperation.IsRowVersion,
                    alterColumnOperation.IsNullable,
                    alterColumnOperation.DefaultValue,
                    alterColumnOperation.DefaultValueSql,
                    alterColumnOperation.ComputedColumnSql,
                    /*identity:*/ false,
                    alterColumnOperation,
                    model,
                    builder);

                builder.AppendLine(Dependencies.SqlGenerationHelper.StatementTerminator);
                builder.EndCommand();
            }
        }
    }
}