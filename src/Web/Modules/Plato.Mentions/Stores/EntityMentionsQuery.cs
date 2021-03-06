﻿using System;
using System.Data;
using System.Text;
using System.Threading.Tasks;
using PlatoCore.Data.Abstractions;
using PlatoCore.Stores.Abstractions;
using Plato.Mentions.Models;

namespace Plato.Mentions.Stores
{

    #region "EntityMentionsQuery"

    public class EntityMentionsQuery : DefaultQuery<EntityMention>
    {

        private readonly IQueryableStore<EntityMention> _store;

        public EntityMentionsQuery(IQueryableStore<EntityMention> store)
        {
            _store = store;
        }

        public EntityMentionsQueryParams Params { get; set; }

        public override IQuery<EntityMention> Select<T>(Action<T> configure)
        {
            var defaultParams = new T();
            configure(defaultParams);
            Params = (EntityMentionsQueryParams)Convert.ChangeType(defaultParams, typeof(EntityMentionsQueryParams));
            return this;
        }

        public override async Task<IPagedResults<EntityMention>> ToList()
        {

            var builder = new EntityMentionsQueryBuilder(this);
            var populateSql = builder.BuildSqlPopulate();
            var countSql = builder.BuildSqlCount();
            var keywords = Params?.Username.Value ?? string.Empty;

            return await _store.SelectAsync(new IDbDataParameter[]
            {
                new DbParam("PageIndex", DbType.Int32, PageIndex),
                new DbParam("PageSize", DbType.Int32, PageSize),
                new DbParam("SqlPopulate", DbType.String, populateSql),
                new DbParam("SqlCount", DbType.String, countSql),
                new DbParam("Username", DbType.String, keywords)
            });

        }

    }

    #endregion

    #region "EntityMentionsQueryParams"

    public class EntityMentionsQueryParams
    {

        private WhereInt _userId;
        private WhereInt _entityId;
        private WhereInt _entityReplyId;
        private WhereString _username;

        public WhereInt UserId
        {
            get => _userId ?? (_userId = new WhereInt());
            set => _userId = value;
        }

        public WhereInt EntityId
        {
            get => _entityId ?? (_entityId = new WhereInt());
            set => _entityId = value;
        }

        public WhereInt EntityReplyId
        {
            get => _entityReplyId ?? (_entityReplyId = new WhereInt());
            set => _entityReplyId = value;
        }

        public WhereString Username
        {
            get => _username ?? (_username = new WhereString());
            set => _username = value;
        }

    }

    #endregion

    #region "EntityMentionsQueryBuilder"

    public class EntityMentionsQueryBuilder : IQueryBuilder
    {

        #region "Constructor"

        private readonly string _entityMentionsTableName;

        private readonly EntityMentionsQuery _query;

        public EntityMentionsQueryBuilder(EntityMentionsQuery query)
        {
            _query = query;
            _entityMentionsTableName = GetTableNameWithPrefix("EntityMentions");
        }

        #endregion

        #region "Implementation"

        public string BuildSqlPopulate()
        {
            var whereClause = BuildWhereClause();
            var orderBy = BuildOrderBy();
            var sb = new StringBuilder();
            sb.Append("SELECT ")
                .Append(BuildPopulateSelect())
                .Append(" FROM ")
                .Append(BuildTables());
            if (!string.IsNullOrEmpty(whereClause))
                sb.Append(" WHERE (").Append(whereClause).Append(")");
            // Order only if we have something to order by
            sb.Append(" ORDER BY ").Append(!string.IsNullOrEmpty(orderBy)
                ? orderBy
                : "(SELECT NULL)");
            // Limit results only if we have a specific page size
            if (!_query.IsDefaultPageSize)
                sb.Append(" OFFSET @RowIndex ROWS FETCH NEXT @PageSize ROWS ONLY;");
            return sb.ToString();
        }

        public string BuildSqlCount()
        {
            if (!_query.CountTotal)
                return string.Empty;
            var whereClause = BuildWhereClause();
            var sb = new StringBuilder();
            sb.Append("SELECT COUNT(em.Id) FROM ")
                .Append(BuildTables());
            if (!string.IsNullOrEmpty(whereClause))
                sb.Append(" WHERE (").Append(whereClause).Append(")");
            return sb.ToString();
        }

        #endregion

        #region "Private Methods"

        private string BuildPopulateSelect()
        {
            var sb = new StringBuilder();
            sb.Append("em.*");
            return sb.ToString();

        }

        private string BuildTables()
        {

            var sb = new StringBuilder();

            sb.Append(_entityMentionsTableName)
                .Append(" em ");

            return sb.ToString();

        }

        private string GetTableNameWithPrefix(string tableName)
        {
            return !string.IsNullOrEmpty(_query.Options.TablePrefix)
                ? _query.Options.TablePrefix + tableName
                : tableName;
        }

        private string BuildWhereClause()
        {
            var sb = new StringBuilder();

            // UserId
            if (_query.Params.UserId.Value > -1)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                    sb.Append(_query.Params.UserId.Operator);
                sb.Append(_query.Params.UserId.ToSqlString("em.UserId"));
            }

            // EntityId
            if (_query.Params.EntityId.Value > -1)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                    sb.Append(_query.Params.EntityId.Operator);
                sb.Append(_query.Params.EntityId.ToSqlString("em.EntityId"));
            }

            // EntityReplyId
            if (_query.Params.EntityReplyId.Value > -1)
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                    sb.Append(_query.Params.EntityReplyId.Operator);
                sb.Append(_query.Params.EntityReplyId.ToSqlString("em.EntityReplyId"));
            }
            
            if (!string.IsNullOrEmpty(_query.Params.Username.Value))
            {
                if (!string.IsNullOrEmpty(sb.ToString()))
                    sb.Append(_query.Params.Username.Operator);
                sb.Append(_query.Params.Username.ToSqlString("Username", "Username"));
            }

            return sb.ToString();

        }

        private string GetQualifiedColumnName(string columnName)
        {
            if (columnName == null)
            {
                throw new ArgumentNullException(nameof(columnName));
            }

            return columnName.IndexOf('.') >= 0
                ? columnName
                : "em." + columnName;
        }

        private string BuildOrderBy()
        {
            if (_query.SortColumns.Count == 0) return null;
            var sb = new StringBuilder();
            var i = 0;
            foreach (var sortColumn in _query.SortColumns)
            {
                sb.Append(GetQualifiedColumnName(sortColumn.Key));
                if (sortColumn.Value != OrderBy.Asc)
                    sb.Append(" DESC");
                if (i < _query.SortColumns.Count - 1)
                    sb.Append(", ");
                i += 1;
            }
            return sb.ToString();
        }

        #endregion

    }

    #endregion

}
