﻿using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Infrastructure;
using Plato.Discuss.Models;
using Plato.Entities.ViewModels;
using PlatoCore.Data.Abstractions;
using PlatoCore.Layout.ViewAdapters.Abstractions;
using Plato.Tags.Models;
using Plato.Tags.Stores;

namespace Plato.Discuss.Tags.ViewAdapters
{

    public class TopicListItemViewAdapter : ViewAdapterProviderBase
    {
      
        private readonly IEntityTagStore<EntityTag> _entityTagStore;       
        private readonly IActionContextAccessor _actionContextAccessor;

        private IDictionary<int, IList<EntityTag>> _lookUpTable;

        public TopicListItemViewAdapter(          
            IEntityTagStore<EntityTag> entityTagStore,
            IActionContextAccessor actionContextAccessor)
        {           
            _entityTagStore = entityTagStore;
            _actionContextAccessor = actionContextAccessor;
            ViewName = "TopicListItem";
        }

        public override async Task<IViewAdapterResult> ConfigureAsync(string viewName)
        {

            if (!viewName.Equals(ViewName, StringComparison.OrdinalIgnoreCase))
            {
                return default(IViewAdapterResult);
            }
            
            // Plato.Discuss does not have a dependency on Plato.Discuss.Tags
            // Instead we update the model for the entity list item view component
            // here via our view adapter to include the tag data for the entity
            // This way the tag data is only ever populated if the tags feature is enabled
            return await AdaptAsync(ViewName, v =>
            {
                v.AdaptModel<EntityListItemViewModel<Topic>>(async model  =>
                {

                    if (_lookUpTable == null)
                    {
                        _lookUpTable = await BuildLookUpTable();
                    }

                    if (_lookUpTable == null)
                    {
                        // Return an anonymous type as we are adapting a view component
                        return new
                        {
                            model
                        };
                    }

                    if (model.Entity == null)
                    {
                        // Return an anonymous type as we are adapting a view component
                        return new
                        {
                            model
                        };
                    }

                    // No need to modify the model if no tags have been found
                    if (!_lookUpTable.ContainsKey(model.Entity.Id))
                    {
                        // Return an anonymous type as we are adapting a view component
                        return new
                        {
                            model
                        };
                    }
                    
                    // Add tags to the model from our dictionary
                    var entityTags = new List<EntityTag>();
                    if (_lookUpTable.ContainsKey(model.Entity.Id))
                    {
                        foreach (var tag in _lookUpTable[model.Entity.Id])
                        {
                            entityTags.Add(tag);
                        }
                    }
                  
                    model.Tags = entityTags;

                    // Return an anonymous type as we are adapting a view component
                    return new
                    {
                        model
                    };

                });
            });

        }
        
        async Task<IDictionary<int, IList<EntityTag>>> BuildLookUpTable()
        {


           // Get index view model from context
           var viewModel = _actionContextAccessor.ActionContext.HttpContext.Items[typeof(EntityIndexViewModel<Topic>)] as EntityIndexViewModel<Topic>;
            if (viewModel == null)
            {
                return null;
            }

            if (viewModel.Results == null)
            {
                return null;
            }

            // Get all entities for our current view
            var entities = viewModel.Results;

            // Get all entity tag relationships for displayed entities
            IPagedResults<EntityTag> entityTags = null;
            if (entities?.Data != null)
            {
                entityTags = await _entityTagStore.QueryAsync()
                    .Select<EntityTagQueryParams>(q =>
                    {
                        q.EntityId.IsIn(entities.Data.Select(e => e.Id).ToArray());
                        q.EntityReplyId.Equals(0);
                    })
                    .ToList();
            }

            // Build a dictionary of entity and tag relationships
            var output = new ConcurrentDictionary<int, IList<EntityTag>>();
            if (entityTags?.Data != null)
            {
                foreach (var entityTag in entityTags.Data)
                {
                    var tag = entityTags.Data.FirstOrDefault(t => t.TagId == entityTag.TagId);
                    if (tag != null)
                    {
                        output.AddOrUpdate(entityTag.EntityId, new List<EntityTag>()
                        {
                            tag
                        }, (k, v) =>
                        {
                            v.Add(tag);
                            return v;
                        });
                    }
                }
            }

            return output;

        }

    }

}
