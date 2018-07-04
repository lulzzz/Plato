﻿using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using Plato.Entities.Models;
using Plato.Entities.Repositories;
using Plato.Internal.Abstractions;
using Plato.Internal.Cache;
using Plato.Internal.Data.Abstractions;
using Plato.Internal.Modules.Abstractions;

namespace Plato.Entities.Stores
{

    public class EntityStore : IEntityStore<Entity>
    {
     
        private string _key = "Entity";

        #region "Constructor"

        private readonly IEntityRepository<Entity> _entityRepository;
        private readonly ILogger<EntityStore> _logger;
        private readonly ICacheDependency _cacheDependency;
        private readonly IMemoryCache _memoryCache;
        private readonly IDbQuery _dbQuery;
        private readonly ITypedModuleProvider _typedModuleProvider;

        public EntityStore(
            ITypedModuleProvider typedModuleProvider,
            IEntityRepository<Entity> entityRepository,
            ICacheDependency cacheDependency,
            ILogger<EntityStore> logger,
            IMemoryCache memoryCache,
            IDbQuery dbQuery)
        {
            _typedModuleProvider = typedModuleProvider;
            _entityRepository = entityRepository;
            _cacheDependency = cacheDependency;
            _memoryCache = memoryCache;
            _dbQuery = dbQuery;
            _logger = logger;
        }

        #endregion

        #region "Implementation"

        public async Task<Entity> CreateAsync(Entity entity)
        {
            
            var data = new List<EntityData>();
            foreach (var item in entity.MetaData)
            {
                data.Add(new EntityData()
                {
                    Key = item.Key.FullName,
                    Value = item.Value.Serialize()
                });
            }
            entity.Data = data;
            
            // Add entity
            var newEntity = await _entityRepository.InsertUpdateAsync(entity);
            if (newEntity != null)
            {
                _cacheDependency.CancelToken(GetEntityCacheKey());
                newEntity = await GetByIdAsync(newEntity.Id);
            }
            
            return newEntity;

        }

        public async Task<Entity> UpdateAsync(Entity entity)
        {
            var output = await _entityRepository.InsertUpdateAsync(entity);
            if (output != null)
            {
                _cacheDependency.CancelToken(GetEntityCacheKey());
            }
            return output;
        }

        public async Task<bool> DeleteAsync(Entity entity)
        {
         
            var success = await _entityRepository.DeleteAsync(entity.Id);
            if (success)
            {
                _cacheDependency.CancelToken(GetEntityCacheKey());
            }
            
            return success;
        }

        public async Task<Entity> GetByIdAsync(int id)
        {
            
            var entity = await _entityRepository.SelectByIdAsync(id);
            if (entity != null)
            {
                foreach (var data in entity.Data)
                {
                    var type = await GetModuleTypeCandidateAsync(data.Key);
                    if (type != null)
                    {
                        var obj = JsonConvert.DeserializeObject(data.Value, type);
                        entity.SetMetaData(type, (ISerializable)obj);
                    }
                }
            }

            return entity;
        }
        
        public IQuery QueryAsync()
        {
            var query = new EntityQuery(this);
            return _dbQuery.ConfigureQuery(query); ;
        }

        public async Task<IPagedResults<T>> SelectAsync<T>(params object[] args) where T : class
        {

            var key = GetEntityCacheKey();
            return await _memoryCache.GetOrCreateAsync(key, async (cacheEntry) =>
            {
                var output = await _entityRepository.SelectAsync<T>(args);
                if (output != null)
                {
                    if (_logger.IsEnabled(LogLevel.Information))
                    {
                        _logger.LogDebug("Adding entry to cache of type {0}. Entry key: {1}.",
                            _memoryCache.GetType().Name, key);
                    }
                }
                cacheEntry.ExpirationTokens.Add(_cacheDependency.GetToken(key));
                return output;
            });

        }

        #endregion

        #region "Private Methods"
        
    
        async Task<Type> GetModuleTypeCandidateAsync(string typeName)
        {
            return await _typedModuleProvider.GetTypeCandidateAsync(typeName, typeof(ISerializable));
        }
        
        private string GetEntityCacheKey()
        {
            return $"{_key}";
        }

        #endregion
        
    }

}