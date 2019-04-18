﻿using System;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Plato.Internal.Stores.Abstractions;

namespace Plato.Internal.Stores
{
    public class FederatedQueryManager<TModel> : IFederatedQueryManager<TModel> where TModel : class
    {

        private IEnumerable<string> _queries;

        private readonly IEnumerable<IFederatedQueryProvider<TModel>> _providers;
        private readonly ILogger<FederatedQueryManager<TModel>> _logger;

        public FederatedQueryManager(
            IEnumerable<IFederatedQueryProvider<TModel>> providers,
            ILogger<FederatedQueryManager<TModel>> logger)
        {
            _providers = providers;
            _logger = logger;
        }

        public IEnumerable<string> GetQueries(IFederatedQueryContext<TModel> context)
        {
            if (_queries == null)
            {
                var queries = new List<string>();
                foreach (var provider in _providers)
                {
                    try
                    {
                        var providedQueries = provider.GetQueries(context);
                        foreach (var query in providedQueries)
                        {
                            queries.Add(ReplaceTablePrefix(query, context.Query.Options.TablePrefix));
                        }
                    }
                    catch (Exception e)
                    {
                        _logger.LogError(e, $"An exception occurred within the search query provider {provider.GetType().Name}. Please review your query provider and try again. {e.Message}");
                        throw;
                    }
                }

                _queries = queries;
            }

            return _queries;

        }

        string ReplaceTablePrefix(string input, string tablePrefix)
        {
            return input.Replace("{prefix}_", tablePrefix);
        }

    }

}