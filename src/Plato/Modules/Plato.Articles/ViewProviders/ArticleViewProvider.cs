﻿using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Plato.Articles.Models;
using Plato.Articles.Services;
using Plato.Entities.Stores;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Abstractions.Extensions;
using Plato.Entities.ViewModels;
using Plato.Entities.Models;
using Plato.Entities.Services;

namespace Plato.Articles.ViewProviders
{
    public class ArticleViewProvider : BaseViewProvider<Article>
    {

        private const string EditorHtmlName = "message";
        
        private readonly IEntityStore<Article> _entityStore;
        private readonly IPostManager<Article> _articleManager;
        private readonly IEntityViewIncrementer<Article> _viewIncrementer;

        private readonly HttpRequest _request;
        
        public ArticleViewProvider(
            IHttpContextAccessor httpContextAccessor,
            IEntityStore<Article> entityStore,
            IPostManager<Article> articleManager,
            IEntityViewIncrementer<Article> viewIncrementer)
        {
            _entityStore = entityStore;
            _articleManager = articleManager;
            _viewIncrementer = viewIncrementer;
            _request = httpContextAccessor.HttpContext.Request;
        }

        public override Task<IViewProviderResult> BuildIndexAsync(Article article, IViewProviderContext context)
        {

            var viewModel = context.Controller.HttpContext.Items[typeof(EntityIndexViewModel<Article>)] as EntityIndexViewModel<Article>;
            if (viewModel == null)
            {
                throw new Exception($"A view model of type {typeof(EntityIndexViewModel<Entity>).ToString()} has not been registered on the HttpContext!");
            }

            return Task.FromResult(Views(
                View<EntityIndexViewModel<Article>>("Home.Index.Header", model => viewModel).Zone("header"),
                View<EntityIndexViewModel<Article>>("Home.Index.Tools", model => viewModel).Zone("tools"),
                View<EntityIndexViewModel<Article>>("Home.Index.Content", model => viewModel).Zone("content")
            ));

        }
        
        public override async Task<IViewProviderResult> BuildDisplayAsync(Article article, IViewProviderContext context)
        {
            
            var viewModel = context.Controller.HttpContext.Items[typeof(EntityViewModel<Article, Comment>)] as EntityViewModel<Article, Comment>;
            if (viewModel == null)
            {
                throw new Exception($"A view model of type {typeof(EntityIndexViewModel<Article>).ToString()} has not been registered on the HttpContext!");
            }

            // Increment entity views
            await _viewIncrementer
                .Contextulize(context.Controller.HttpContext)
                .IncrementAsync(article);

            return Views(
                View<Article>("Home.Display.Header", model => article).Zone("header"),
                View<Article>("Home.Display.Tools", model => article).Zone("tools"),
                View<Article>("Home.Display.Sidebar", model => article).Zone("sidebar"),
                View<EntityViewModel<Article, Comment>>("Home.Display.Content", model => viewModel).Zone("content"),
                View<EditEntityReplyViewModel>("Home.Display.Footer", model => new EditEntityReplyViewModel()
                {
                    EntityId = article.Id,
                    EditorHtmlName = EditorHtmlName
                }).Zone("footer"),
                View<EntityViewModel<Article, Comment>>("Home.Display.Actions", model => viewModel)
                    .Zone("actions")
                    .Order(int.MaxValue)

            );

        }
        
        public override Task<IViewProviderResult> BuildEditAsync(Article article, IViewProviderContext updater)
        {

            // Ensures we persist the message between post backs
            var message = article.Message;
            if (_request.Method == "POST")
            {
                foreach (string key in _request.Form.Keys)
                {
                    if (key == EditorHtmlName)
                    {
                        message = _request.Form[key];
                    }
                }
            }
          
            var viewModel = new EditEntityViewModel()
            {
                Id = article.Id,
                Title = article.Title,
                Message = message,
                EditorHtmlName = EditorHtmlName,
                Alias = article.Alias
            };
     
            return Task.FromResult(Views(
                View<EditEntityViewModel>("Home.Edit.Header", model => viewModel).Zone("header"),
                View<EditEntityViewModel>("Home.Edit.Content", model => viewModel).Zone("content"),
                View<EditEntityViewModel>("Home.Edit.Footer", model => viewModel).Zone("Footer")
            ));

        }
        
        public override async Task<bool> ValidateModelAsync(Article article, IUpdateModel updater)
        {
            return await updater.TryUpdateModelAsync(new EditEntityViewModel
            {
                Title = article.Title,
                Message = article.Message
            });
        }

        public override async Task ComposeTypeAsync(Article article, IUpdateModel updater)
        {

            var model = new EditEntityViewModel
            {
                Title = article.Title,
                Message = article.Message
            };

            await updater.TryUpdateModelAsync(model);

            if (updater.ModelState.IsValid)
            {

                article.Title = model.Title;
                article.Message = model.Message;
            }

        }
        
        public override async Task<IViewProviderResult> BuildUpdateAsync(Article article, IViewProviderContext context)
        {
            
            if (article.IsNewTopic)
            {
                return default(IViewProviderResult);
            }

            var entity = await _entityStore.GetByIdAsync(article.Id);
            if (entity == null)
            {
                return await BuildIndexAsync(article, context);
            }
            
            // Validate 
            if (await ValidateModelAsync(article, context.Updater))
            {
                // Update
                var result = await _articleManager.UpdateAsync(article);

                // Was there a problem updating the entity?
                if (!result.Succeeded)
                {
                    foreach (var error in result.Errors)
                    {
                        context.Updater.ModelState.AddModelError(string.Empty, error.Description);
                    }
                }

            }

            return await BuildEditAsync(article, context);

        }

    }

}
