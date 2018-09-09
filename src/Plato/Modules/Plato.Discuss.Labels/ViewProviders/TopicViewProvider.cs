﻿using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Localization;
using Plato.Entities.Stores;
using Plato.Internal.Hosting.Abstractions;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Layout.ModelBinding;
using Plato.Labels.Models;
using Plato.Labels.Stores;
using Plato.Discuss.Labels.ViewModels;
using Plato.Discuss.Models;
using Plato.Internal.Shell.Abstractions;

namespace Plato.Discuss.Labels.ViewProviders
{
    public class TopicViewProvider : BaseViewProvider<Topic>
    {

        private const string LabelHtmlName = "label";

        private readonly ILabelStore<Models.Label> _labelStore;
        private readonly IEntityLabelStore<EntityLabel> _entityLabelStore;
        private readonly IEntityStore<Topic> _entityStore;
        private readonly IContextFacade _contextFacade;
        private readonly IStringLocalizer T;
        
        private readonly HttpRequest _request;

        public TopicViewProvider(
            IContextFacade contextFacade,
            ILabelStore<Models.Label> labelStore, 
            IEntityStore<Topic> entityStore,
            IHttpContextAccessor httpContextAccessor,
            IEntityLabelStore<EntityLabel> entityLabelStore,
            IStringLocalizer<TopicViewProvider> stringLocalize)
        {
            _contextFacade = contextFacade;
            _labelStore = labelStore;
            _entityStore = entityStore;
            _entityLabelStore = entityLabelStore;
            _request = httpContextAccessor.HttpContext.Request;
            T = stringLocalize;
        }

        #region "Implementation"

        public override async Task<IViewProviderResult> BuildIndexAsync(Topic viewModel, IUpdateModel updater)
        {

            // Ensure we explictly set the featureId
            var feature = await _contextFacade.GetFeatureByModuleIdAsync("Plato.Discuss.Labels");
            if (feature == null)
            {
                return default(IViewProviderResult);
            }

            var labels = await _labelStore.GetByFeatureIdAsync(feature.Id);
            
            return Views(View<LabelsViewModel>("Discuss.Labels.Index.Sidebar", model =>
                {
                    model.Labels = labels;
                    return model;
                }).Zone("sidebar").Order(2)
            );
            

        }

        public override async Task<IViewProviderResult> BuildDisplayAsync(Topic viewModel, IUpdateModel updater)
        {

            var feature = await _contextFacade.GetFeatureByModuleIdAsync("Plato.Discuss.Tags");
            if (feature == null)
            {
                return default(IViewProviderResult);
            }

            var categories = await _labelStore.GetByFeatureIdAsync(feature.Id);
            
            return Views(
                View<LabelsViewModel>("Discuss.Labels.Index.Sidebar", model =>
                {
                    model.Labels = categories;
                    return model;
                }).Zone("sidebar").Order(1)
            );

        }
        
        public override async Task<IViewProviderResult> BuildEditAsync(Topic topic, IUpdateModel updater)
        {
            var viewModel = new EditTopicLabelsViewModel()
            {
                HtmlName = LabelHtmlName,
                SelectedLabels = await GetLabelIdsByEntityIdAsync(topic.Id)
            };

            return Views(
                View<EditTopicLabelsViewModel>("Discuss.Labels.Edit.Sidebar", model => viewModel)
                    .Zone("sidebar")
                    .Order(2)
            );

        }
        
        public override async Task<bool> ValidateModelAsync(Topic topic, IUpdateModel updater)
        {
         
            // Build model
            var model = new EditTopicLabelsViewModel();
            model.SelectedLabels = GetLabelsToAdd(); ;
         
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
                var labelsToAdd = GetLabelsToAdd();
                if (labelsToAdd != null)
                {

                    // Build channels to remove
                    var labelsToRemove = new List<int>();
                    foreach (var role in await GetLabelIdsByEntityIdAsync(topic.Id))
                    {
                        if (!labelsToAdd.Contains(role))
                        {
                            labelsToRemove.Add(role);
                        }
                    }

                    // Remove channels
                    foreach (var channelId in labelsToRemove)
                    {
                        await _entityLabelStore.DeleteByEntityIdAndLabelId(topic.Id, channelId);
                    }
                    
                    var user = await _contextFacade.GetAuthenticatedUserAsync();

                    // Add new entity categories
                    foreach (var labelId in labelsToAdd)
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
        
        List<int> GetLabelsToAdd()
        {
            // Build selected channels
            List<int> labelsToAdd = null;
            foreach (var key in _request.Form.Keys)
            {
                if (key.StartsWith(LabelHtmlName))
                {
                    if (labelsToAdd == null)
                    {
                        labelsToAdd = new List<int>();
                    }
                    var values = _request.Form[key];
                    foreach (var value in values)
                    {
                        int.TryParse(value, out var id);
                        if (!labelsToAdd.Contains(id))
                        {
                            labelsToAdd.Add(id);
                        }
                    }
                }
            }

            return labelsToAdd;
        }

        async Task<IEnumerable<int>> GetLabelIdsByEntityIdAsync(int entityId)
        {

            if (entityId == 0)
            {
                // return empty collection for new topics
                return new List<int>();
            }

            var labels = await _entityLabelStore.GetByEntityId(entityId);
            if (labels != null)
            {
                return labels.Select(s => s.LabelId).ToArray();
            }

            return new List<int>();

        }

        #endregion



    }
}
