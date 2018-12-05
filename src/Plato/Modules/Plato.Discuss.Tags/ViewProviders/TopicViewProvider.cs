﻿using System;
using System.Linq;
using Newtonsoft.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Discuss.Models;
using Plato.Discuss.Tags.ViewModels;
using Plato.Entities.Stores;
using Plato.Internal.Abstractions.Extensions;
using Plato.Internal.Data.Abstractions;
using Plato.Internal.Features.Abstractions;
using Plato.Tags.Models;
using Plato.Tags.Services;
using Plato.Tags.Stores;

namespace Plato.Discuss.Tags.ViewProviders
{
    public class TopicViewProvider : BaseViewProvider<Topic>
    {
        private const string TagsHtmlName = "tags";

        private readonly ITagStore<Tag> _tagStore;
        private readonly IEntityTagStore<EntityTag> _entityTagStore;
        private readonly IEntityStore<Topic> _entityStore;
        private readonly IEntityTagManager<EntityTag> _entityTagManager;
        private readonly ITagManager<Tag> _tagManager;
        private readonly IFeatureFacade _featureFacade;

        private readonly HttpRequest _request;
        
        public TopicViewProvider(
            ITagStore<Tag> tagStore,
            IEntityStore<Topic> entityStore,
            IEntityTagStore<EntityTag> entityTagStore,
            IHttpContextAccessor httpContextAccessor, 
            IEntityTagManager<EntityTag> entityTagManager,
            ITagManager<Tag> tagManager, 
            IFeatureFacade featureFacade)
        {
            _tagStore = tagStore;
            _entityStore = entityStore;
            _entityTagStore = entityTagStore;
            _entityTagManager = entityTagManager;
            _tagManager = tagManager;
            _featureFacade = featureFacade;
            _request = httpContextAccessor.HttpContext.Request;
        }

        #region "Implementation"

        public override Task<IViewProviderResult> BuildDisplayAsync(Topic viewModel, IViewProviderContext context)
        {
            return Task.FromResult(default(IViewProviderResult));
        }

        public override Task<IViewProviderResult> BuildIndexAsync(Topic viewModel, IViewProviderContext context)
        {
            return Task.FromResult(default(IViewProviderResult));
        }

        public override async Task<IViewProviderResult> BuildEditAsync(Topic topic, IViewProviderContext context)
        {

            //var entityTags = await GetEntityTagsByEntityIdAsync(topic.Id);


            // Get all entity tags

            var tags = "";
      
            var entityTags = await GetEntityTagsByEntityIdAsync(topic.Id);
            if (entityTags != null)
            {

                var existingTags = await _tagStore.QueryAsync()
                    .Select<TagQueryParams>(q =>
                    {
                        q.Id.IsIn(entityTags.Select(e => e.TagId).ToArray());
                    })
                    .OrderBy("Name")
                    .ToList();

                List<TagApiResult> tagsToSerialize = null;
                if (existingTags != null)
                {
                    tagsToSerialize = new List<TagApiResult>();
                    foreach (var tag in existingTags.Data)
                    {
                        tagsToSerialize.Add(new TagApiResult()
                        {
                            Id = tag.Id,
                            Name = tag.Name
                        });
                    }
                }

                if (tagsToSerialize != null)
                {
                    tags = tagsToSerialize.Serialize();
                }
            
            }
            
            //var entityTagsList = entityTags.ToList();
            
            
            var viewModel = new EditTopicTagsViewModel()
            {
                Tags = tags,
                HtmlName = TagsHtmlName
            };

            return Views(
                View<EditTopicTagsViewModel>("Topic.Tags.Edit.Footer", model => viewModel).Zone("content")
                    .Order(int.MaxValue)
            );
            
        }
        
        public override Task<bool> ValidateModelAsync(Topic topic, IUpdateModel updater)
        {
            // ensure tags are optional
            return Task.FromResult(true);
        }

        public override async Task<IViewProviderResult> BuildUpdateAsync(Topic topic, IViewProviderContext context)
        {
            // Ensure entity exists before attempting to update
            var entity = await _entityStore.GetByIdAsync(topic.Id);
            if (entity == null)
            {
                return await BuildIndexAsync(topic, context);
            }

            // Validate model
            if (await ValidateModelAsync(topic, context.Updater))
            {

                // Get selected tags
                var tagsToAdd = await GetTagsToAddAsync(topic);

                // Build tags to remove
                var tagsToRemove = new List<EntityTag>();

                // Iterate over existing tags
                foreach (var entityTag in await GetEntityTagsByEntityIdAsync(topic.Id))
                {
                    // Is our existing tag in our list of new tags to add
                    var existingTag = tagsToAdd.FirstOrDefault(t => t.Id == entityTag.TagId);
                    if (existingTag != null)
                    {
                        tagsToAdd.Remove(existingTag);
                    }
                    else
                    {
                        // Entry does NOT exist in tags so add ensure it's removed
                        tagsToRemove.Add(entityTag);
                    }
                }

                // Remove entity tags
                foreach (var entityTag in tagsToRemove)
                {
                    var result = await _entityTagManager.DeleteAsync(entityTag);
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            context.Updater.ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }

                // Add new entity labels
                foreach (var tag in tagsToAdd)
                {
                    var result = await _entityTagManager.CreateAsync(new EntityTag()
                    {
                        EntityId = topic.Id,
                        TagId = tag.Id
                    });
                    if (!result.Succeeded)
                    {
                        foreach (var error in result.Errors)
                        {
                            context.Updater.ModelState.AddModelError(string.Empty, error.Description);
                        }
                    }
                }

            }

            return await BuildEditAsync(topic, context);

        }

        #endregion

        #region "Private Methods"

        async Task<List<Tag>> GetTagsToAddAsync(Topic topic)
        {
            
            var feature = await _featureFacade.GetFeatureByIdAsync("Plato.Discuss");
            var featureId = feature?.Id ?? 0;

            var tagsToAdd = new List<Tag>();
            foreach (var key in _request.Form.Keys)
            {
                if (key.Equals(TagsHtmlName))
                {
                    var value = _request.Form[key];
                    if (!String.IsNullOrEmpty(value))
                    {

                        var items = JsonConvert.DeserializeObject<IEnumerable<TagApiResult>>(value);
                        foreach (var item in items)
                        {

                            if (item.Id > 0)
                            {
                                // We've added a tag that already exists
                                var tag = await _tagStore.GetByIdAsync(item.Id);
                                if (tag != null)
                                {
                                    tagsToAdd.Add(tag);
                                }
                            }
                            else
                            {
                                // We've added a new tag
                                var tagManagerResult = await _tagManager.CreateAsync(new Tag()
                                {
                                    FeatureId = featureId,
                                    Name = item.Name
                                });
                                if (tagManagerResult.Succeeded)
                                {
                                    // Add entity tag relationship
                                    var entityTagManagerResult = await _entityTagManager.CreateAsync(new EntityTag()
                                    {
                                        EntityId = topic.Id,
                                        TagId = tagManagerResult.Response.Id
                                    });
                                    if (entityTagManagerResult.Succeeded)
                                    {
                                        tagsToAdd.Add(tagManagerResult.Response);
                                    }
                                }
                            }

                        }

                    }

                }

            }

            return tagsToAdd;
        }


        async Task<IEnumerable<EntityTag>> GetEntityTagsByEntityIdAsync(int entityId)
        {

            if (entityId == 0)
            {
                // return empty collection for new topics
                return null;
            }
            
            return await _entityTagStore.GetByEntityId(entityId);

        }

        #endregion

    }

}
