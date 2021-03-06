﻿using System.Data;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using PlatoCore.Data.Schemas.Abstractions;
using PlatoCore.Features.Abstractions;
using PlatoCore.Security.Abstractions;

namespace Plato.Attachments.Handlers
{

    public class FeatureEventHandler : BaseFeatureEventHandler
    {

        public string Version { get; } = "1.0.0";


        private readonly SchemaTable _attachments = new SchemaTable()
        {
            Name = "Attachments",
            Columns = new List<SchemaColumn>()
                {
                    new SchemaColumn()
                    {
                        PrimaryKey = true,
                        Name = "Id",
                        DbType = DbType.Int32
                    },
                    new SchemaColumn()
                    {
                        Name = "[Name]",
                        Length = "255",
                        DbType = DbType.String
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentBlob",
                        Nullable = true,
                        DbType = DbType.Binary
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentType",
                        Length = "75",
                        DbType = DbType.String
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentLength",
                        DbType = DbType.Int64
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentGuid",
                        Length = "32",
                        DbType = DbType.String
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentCheckSum",
                        Length = "32",
                        DbType = DbType.String
                    },
                    new SchemaColumn()
                    {
                        Name = "TotalViews",
                        DbType = DbType.Int32
                    },
                    new SchemaColumn()
                    {
                        Name = "CreatedUserId",
                        DbType = DbType.Int32
                    },
                    new SchemaColumn()
                    {
                        Name = "CreatedDate",
                        DbType = DbType.DateTimeOffset
                    },
                    new SchemaColumn()
                    {
                        Name = "ModifiedUserId",
                        DbType = DbType.Int32
                    },
                    new SchemaColumn()
                    {
                        Name = "ModifiedDate",
                        DbType = DbType.DateTimeOffset,
                        Nullable = true
                    }
                }
        };

        private readonly IDefaultRolesManager _defaultRolesManager;
        private readonly ISchemaBuilder _schemaBuilder;
        private readonly ISchemaManager _schemaManager;

        public FeatureEventHandler(
            IDefaultRolesManager defaultRolesManager,
            ISchemaBuilder schemaBuilder,
            ISchemaManager schemaManager)
        {
            _defaultRolesManager = defaultRolesManager;
            _schemaBuilder = schemaBuilder;
            _schemaManager = schemaManager;
        }
        
        public override async Task InstallingAsync(IFeatureEventContext context)
        {

            if (context.Logger.IsEnabled(LogLevel.Information))
                context.Logger.LogInformation($"InstallingAsync called within {ModuleId}");

            //var schemaBuilder = context.ServiceProvider.GetRequiredService<ISchemaBuilder>();
            using (var builder = _schemaBuilder)
            {

                // configure
                Configure(builder);

                // Attachments schema
                Attachments(builder);
                
                // Log statements to execute
                if (context.Logger.IsEnabled(LogLevel.Information))
                {
                    context.Logger.LogInformation($"The following SQL statements will be executed...");
                    foreach (var statement in builder.Statements)
                    {
                        context.Logger.LogInformation(statement);
                    }
                }

                // Execute statements
                var errors = await _schemaManager.ExecuteAsync(builder.Statements);
                foreach (var error in errors)
                {
                    context.Errors.Add(error, $"InstallingAsync within {this.GetType().FullName}");
                }

            }

        }

        public override async Task InstalledAsync(IFeatureEventContext context)
        {
            // Apply default permissions to default roles for new feature
            await _defaultRolesManager.UpdateDefaultRolesAsync(new Permissions());
        }

        public override async Task UpdatedAsync(IFeatureEventContext context)
        {
            // Apply any additional permissions to default roles for updated feature
            await _defaultRolesManager.UpdateDefaultRolesAsync(new Permissions());
        }

        public override async Task UninstallingAsync(IFeatureEventContext context)
        {
            if (context.Logger.IsEnabled(LogLevel.Information))
                context.Logger.LogInformation($"UninstallingAsync called within {ModuleId}");

            using (var builder = _schemaBuilder)
            {

                builder.TableBuilder.DropTable(_attachments);

                builder.ProcedureBuilder
                    .DropDefaultProcedures(_attachments)
                    .DropProcedure(new SchemaProcedure("SelectAttachmentsPaged"))
                    .DropProcedure(new SchemaProcedure("UpdateAttachmentContentGuidById"));

                // Log statements to execute
                if (context.Logger.IsEnabled(LogLevel.Information))
                {
                    context.Logger.LogInformation($"The following SQL statements will be executed...");
                    foreach (var statement in builder.Statements)
                    {
                        context.Logger.LogInformation(statement);
                    }
                }

                // Execute statements
                var errors = await _schemaManager.ExecuteAsync(builder.Statements);
                foreach (var error in errors)
                {
                    context.Logger.LogCritical(error, $"An error occurred within the UninstallingAsync method within {this.GetType().FullName}");
                    context.Errors.Add(error, $"UninstallingAsync within {this.GetType().FullName}");
                }

            }

        }

        public override Task UninstalledAsync(IFeatureEventContext context)
        {
            return Task.CompletedTask;
        }
        
        void Configure(ISchemaBuilder builder)
        {

            builder
                .Configure(options =>
                {
                    options.ModuleName = ModuleId;
                    options.Version = Version;
                    options.DropTablesBeforeCreate = true;
                    options.DropProceduresBeforeCreate = true;
                });

        }

        void Attachments(ISchemaBuilder builder)
        {
            
            builder.TableBuilder.CreateTable(_attachments);

            builder.ProcedureBuilder
                .CreateDefaultProcedures(_attachments)
                .CreateProcedure(new SchemaProcedure("SelectAttachmentsPaged", StoredProcedureType.SelectPaged)
                .ForTable(_attachments)
                .WithParameters(new List<SchemaColumn>()
                {
                    new SchemaColumn()
                    {
                        Name = "ContentGuid",
                        DbType = DbType.String,
                        Length = "32"
                    },
                    new SchemaColumn()
                    {
                        Name = "ContentCheckSum",
                        DbType = DbType.String,
                        Length = "32"
                    },                     
                    new SchemaColumn()
                    {
                        Name = "Keywords",
                        DbType = DbType.String,
                        Length = "255"
                    }
                }));

            // UpdateAttachmentContentGuidById
            builder.ProcedureBuilder.CreateProcedure(
                    new SchemaProcedure(
                            $"UpdateAttachmentContentGuidById",
                            @"UPDATE {prefix}_Attachments SET
                                    ContentGuid = @ContentGuid
                                WHERE (
                                    Id = @Id
                                );")
                        .ForTable(_attachments)
                        .WithParameters(new List<SchemaColumn>()
                        {
                             new SchemaColumn()
                            {
                                Name = "Id",
                                DbType = DbType.Int32,                                
                            },
                            new SchemaColumn()
                            {
                                Name = "ContentGuid",
                                DbType = DbType.String,
                                Length = "50"
                            }
                        }));

            // Indexes
            builder.IndexBuilder.CreateIndex(new SchemaIndex()
            {
                TableName = _attachments.Name,
                Columns = new string[]
                {
                    "[Name]",
                    "ContentGuid",
                    "ContentCheckSum"
                }
            });


        }

    }

}
