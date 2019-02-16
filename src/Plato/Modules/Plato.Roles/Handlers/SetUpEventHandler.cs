﻿using System;
using System.Collections.Generic;
using System.Data;
using System.Threading.Tasks;
using Plato.Internal.Abstractions.SetUp;
using Plato.Internal.Data.Schemas.Abstractions;
using Plato.Internal.Security.Abstractions;

namespace Plato.Roles.Handlers
{
    public class SetUpEventHandler : BaseSetUpEventHandler
    {

        public const string Version = "1.0.0";
        
        // Roles schema
        private readonly SchemaTable _roles = new SchemaTable()
        {
            Name = "Roles",
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
                    Name = "NormalizedName",
                    Length = "255",
                    DbType = DbType.String
                },
                new SchemaColumn()
                {
                    Name = "Description",
                    Length = "255",
                    DbType = DbType.String
                },
                new SchemaColumn()
                {
                    Name = "Claims",
                    Length = "max",
                    DbType = DbType.String
                },
                new SchemaColumn()
                {
                    Name = "CreatedDate",
                    DbType = DbType.DateTimeOffset
                },
                new SchemaColumn()
                {
                    Name = "CreatedUserId",
                    DbType = DbType.Int32
                },
                new SchemaColumn()
                {
                    Name = "ModifiedDate",
                    DbType = DbType.DateTimeOffset
                },
                new SchemaColumn()
                {
                    Name = "ModifiedUserId",
                    DbType = DbType.Int32
                },
                new SchemaColumn()
                {
                    Name = "ConcurrencyStamp",
                    Length = "255",
                    DbType = DbType.String
                }
            }
        };

        // User Roles Schema
        private readonly SchemaTable _userRoles = new SchemaTable()
        {
            Name = "UserRoles",
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
                    Name = "UserId",
                    DbType = DbType.Int32
                },
                new SchemaColumn()
                {
                    Name = "RoleId",
                    DbType = DbType.Int32
                }
            }
        };
        
        private readonly ISchemaBuilder _schemaBuilder;
        private readonly IDefaultRolesManager _defaultRolesManager;

        public SetUpEventHandler(
            ISchemaBuilder schemaBuilder,
            IDefaultRolesManager defaultRolesManager)
        {
            _schemaBuilder = schemaBuilder;
            _defaultRolesManager = defaultRolesManager;
        }

        #region "Implementation"

        public override async Task SetUp(SetUpContext context, Action<string, string> reportError)
        {
            
            using (var builder = _schemaBuilder)
            {

                // configure
                Configure(builder);

                // roles 
                Roles(builder);

                // user roles 
                UserRoles(builder);
                
                var result = await builder.ApplySchemaAsync();
                if (result.Errors.Count > 0)
                {
                    foreach (var error in result.Errors)
                    {
                        reportError(error.Message, error.StackTrace);
                    }

                }
                
            }
            
            // Install default roles & permissions on first set-up
            await _defaultRolesManager.InstallDefaultRolesAsync();
            
        }
        
        #endregion

        #region "Private Methods"

        void Configure(ISchemaBuilder builder)
        {

            builder
                .Configure(options =>
                {
                    options.ModuleName = base.ModuleId;
                    options.Version = Version;
                    options.DropTablesBeforeCreate = true;
                    options.DropProceduresBeforeCreate = true;
                });

        }

        void Roles(ISchemaBuilder builder)
        {

            // create tables and default procedures
            builder
                // Create tables
                .CreateTable(_roles)
                // Create basic default CRUD procedures
                .CreateDefaultProcedures(_roles)

                // create unique stored procedures

                .CreateProcedure(
                    new SchemaProcedure("SelectRoleByName", StoredProcedureType.SelectByKey)
                        .ForTable(_roles)
                        .WithParameter(new SchemaColumn()
                        {
                            Name = "[Name]",
                            DbType = DbType.String,
                            Length = "255"
                        }))
                .CreateProcedure(
                    new SchemaProcedure("SelectRoleByNameNormalized", StoredProcedureType.SelectByKey)
                        .ForTable(_roles)
                        .WithParameter(new SchemaColumn()
                        {
                            Name = "NormalizedName",
                            DbType = DbType.String,
                            Length = "255"
                        }))
                .CreateProcedure(new SchemaProcedure("SelectRolesPaged", StoredProcedureType.SelectPaged)
                    .ForTable(_roles)
                    .WithParameters(new List<SchemaColumn>()
                    {
                        new SchemaColumn()
                        {
                            Name = "Id",
                            DbType = DbType.Int32
                        },
                        new SchemaColumn()
                        {
                            Name = "Keywords",
                            DbType = DbType.String,
                            Length = "255"
                        }
                    }));

        }

        void UserRoles(ISchemaBuilder builder)
        {

            // create tables and default procedures
            builder
                // Create tables
                .CreateTable(_userRoles)
                // Create basic default CRUD procedures
                .CreateDefaultProcedures(_userRoles);

            builder
                .CreateProcedure(
                    new SchemaProcedure("SelectRolesByUserId", @"
                            SELECT * FROM {prefix}_Roles WITH (nolock) WHERE Id IN (
	                            SELECT RoleId FROM {prefix}_UserRoles WITH (nolock) 
	                            WHERE (
	                               UserId = @UserId
	                            )
                            )
                        ")
                        .WithParameter(new SchemaColumn()
                        {
                            Name = "UserId",
                            DbType = DbType.Int32
                        }))

                .CreateProcedure(
                    new SchemaProcedure("SelectUserRolesByUserId", StoredProcedureType.SelectByKey)
                        .ForTable(_userRoles)
                        .WithParameter(new SchemaColumn()
                        {
                            Name = "UserId",
                            DbType = DbType.Int32
                        }))

                .CreateProcedure(
                    new SchemaProcedure("DeleteUserRolesByUserId", StoredProcedureType.DeleteByKey)
                        .ForTable(_userRoles)
                        .WithParameter(new SchemaColumn()
                        {
                            Name = "UserId",
                            DbType = DbType.Int32
                        }))
                .CreateProcedure(
                    new SchemaProcedure("DeleteUserRoleByUserIdAndRoleId", StoredProcedureType.DeleteByKey)
                        .ForTable(_userRoles)
                        .WithParameters(new List<SchemaColumn>()
                        {
                            new SchemaColumn()
                            {
                                Name = "UserId",
                                DbType = DbType.Int32
                            },
                            new SchemaColumn()
                            {
                                Name = "RoleId",
                                DbType = DbType.Int32
                            }
                        }));

        }

        #endregion

    }
}
