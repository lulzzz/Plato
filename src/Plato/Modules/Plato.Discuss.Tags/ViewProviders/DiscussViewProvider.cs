﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Plato.Discuss.Tags.Models;
using Plato.Discuss.Tags.ViewModels;
using Plato.Discuss.Models;
using Plato.Discuss.Services;
using Plato.Entities.Stores;
using Plato.Internal.Abstractions;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Layout.ModelBinding;
using Plato.Labels.Models;
using Plato.Labels.Stores;

namespace Plato.Discuss.Tags.ViewProviders
{
    public class DiscussViewProvider : BaseViewProvider<Topic>
    {

        private const string ChannelHtmlName = "channel";

        private readonly ILabelStore<Tag> _tagStore;
        private readonly IEntityLabelStore<EntityLabel> _entityLabelStore;
        private readonly IEntityStore<Topic> _entityStore;
        private readonly IContextFacade _contextFacade;
        private readonly IStringLocalizer T;


        private readonly HttpRequest _request;

        public DiscussViewProvider(
            IContextFacade contextFacade,
            ILabelStore<Tag> tagStore, 
            IEntityStore<Topic> entityStore,
            IHttpContextAccessor httpContextAccessor,
            IEntityLabelStore<EntityLabel> entityLabelStore,
            IStringLocalizer<DiscussViewProvider> stringLocalize)
        {
            _contextFacade = contextFacade;
            _tagStore = tagStore;
            _entityStore = entityStore;
            _entityLabelStore = entityLabelStore;
            _request = httpContextAccessor.HttpContext.Request;
            T = stringLocalize;
        }

        #region "Implementation"

        public override async Task<IViewProviderResult> BuildIndexAsync(Topic viewModel, IUpdateModel updater)
        {

            // Ensure we explictly set the featureId
            var feature = await _contextFacade.GetFeatureByModuleIdAsync("Plato.Discuss.Tags");
            if (feature == null)
            {
                return default(IViewProviderResult);
            }

            var tags = await _tagStore.GetByFeatureIdAsync(feature.Id);
            
            return Views(View<TagsViewModel>("Discuss.Tags.Index.Sidebar", model =>
                {
                    model.Channels = tags;
                    return model;
                }).Zone("sidebar").Order(1)
            );
            

        }

        public override async Task<IViewProviderResult> BuildDisplayAsync(Topic viewModel, IUpdateModel updater)
        {

            var feature = await _contextFacade.GetFeatureByModuleIdAsync("Plato.Discuss.Tags");
            if (feature == null)
            {
                return default(IViewProviderResult);
            }

            var categories = await _tagStore.GetByFeatureIdAsync(feature.Id);
            
            return Views(
                View<TagsViewModel>("Discuss.Index.Sidebar", model =>
                {
                    model.Channels = categories;
                    return model;
                }).Zone("sidebar").Order(1)
            );

        }


        public override async Task<IViewProviderResult> BuildEditAsync(Topic topic, IUpdateModel updater)
        {
            var viewModel = new EditTopicTagsViewModel()
            {
                HtmlName = ChannelHtmlName,
                SelectedChannels = await GetLabelIdsByEntityIdAsync(topic.Id)
            };

            return Views(
                View<EditTopicTagsViewModel>("Discuss.Edit.Sidebar", model => viewModel).Zone("sidebar").Order(1)
            );

        }


        public override async Task<bool> ValidateModelAsync(Topic topic, IUpdateModel updater)
        {
         
            // Build model
            var model = new EditTopicTagsViewModel();
            model.SelectedChannels = GetChannelsToAdd(); ;
         
            // Validate model
            return await updater.TryUpdateModelAsync(model);
        }

        public override async Task<IViewProviderResult> BuildUpdateAsync(Topic topic, IUpdateModel updater)
        {

            // Ensure entity exists before attempting to update
            var entity = await _entityStore.GetByIdAsync(topic.Id);
            if (entity == null)
            {
                return await BuildIndexAsync(topic, updater);
            }

            // Validate model
            if (await ValidateModelAsync(topic, updater))
            {
               
                // Get selected channels
                var channelsToAdd = GetChannelsToAdd();
                if (channelsToAdd != null)
                {

                    // Build channels to remove
                    var channelsToRemove = new List<int>();
                    foreach (var role in await GetLabelIdsByEntityIdAsync(topic.Id))
                    {
                        if (!channelsToAdd.Contains(role))
                        {
                            channelsToRemove.Add(role);
                        }
                    }

                    // Remove channels
                    foreach (var channelId in channelsToRemove)
                    {
                        await _entityLabelStore.DeleteByEntityIdAndLabelId(topic.Id, channelId);
                    }
                    
                    var user = await _contextFacade.GetAuthenticatedUserAsync();

                    // Add new entity categories
                    foreach (var labelId in channelsToAdd)
                    {
                        await _entityLabelStore.CreateAsync(new EntityLabel()
                        {
                            EntityId = topic.Id,
                            LabelId = labelId,
                            CreatedUserId = user?.Id ?? 0,
                            ModifiedUserId = user?.Id ?? 0,
                        });
                    }

                }

            }
           
            return await BuildEditAsync(topic, updater);

        }


        #endregion

        #region "Private Methods"
        
        List<int> GetChannelsToAdd()
        {
            // Build selected channels
            List<int> channelsToAdd = null;
            foreach (var key in _request.Form.Keys)
            {
                if (key.StartsWith(ChannelHtmlName))
                {
                    if (channelsToAdd == null)
                    {
                        channelsToAdd = new List<int>();
                    }
                    var values = _request.Form[key];
                    foreach (var value in values)
                    {
                        int.TryParse(value, out var id);
                        if (!channelsToAdd.Contains(id))
                        {
                            channelsToAdd.Add(id);
                        }
                    }
                }
            }

            return channelsToAdd;
        }

        async Task<IEnumerable<int>> GetLabelIdsByEntityIdAsync(int entityId)
        {

            if (entityId == 0)
            {
                // return empty collection for new topics
                return new List<int>();
            }

            var channels = await _entityLabelStore.GetByEntityId(entityId);
            ;
            if (channels != null)
            {
                return channels.Select(s => s.LabelId).ToArray();
            }

            return new List<int>();

        }

        #endregion



    }
}