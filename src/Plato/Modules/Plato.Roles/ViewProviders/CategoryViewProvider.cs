﻿using System.Linq;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Plato.Categories.Models;
using Plato.Internal.Layout.ModelBinding;
using Plato.Internal.Layout.ViewProviders;
using Plato.Internal.Models.Users;
using Plato.Internal.Stores.Abstractions.Roles;
using Plato.Roles.ViewModels;

namespace Plato.Roles.ViewProviders
{
    public class CategoryViewProvider : BaseViewProvider<Category>
    {

        private const string HtmlName = "UserRoles";

        private readonly UserManager<User> _userManager;
        private readonly IPlatoRoleStore _platoRoleStore;
        
        private readonly HttpRequest _request;

        public CategoryViewProvider(
            UserManager<User> userManager,
            IPlatoRoleStore platoRoleStore,
            IHttpContextAccessor httpContextAccessor)
        {
            _userManager = userManager;
            _platoRoleStore = platoRoleStore;
            _request = httpContextAccessor.HttpContext.Request;
        }


        public override Task<IViewProviderResult> BuildDisplayAsync(Category user, IUpdateModel updater)
        {
            return Task.FromResult(default(IViewProviderResult));
        }

        public override Task<IViewProviderResult> BuildIndexAsync(Category user, IUpdateModel updater)
        {
            return Task.FromResult(default(IViewProviderResult));
        }

        public override async Task<IViewProviderResult> BuildEditAsync(Category user, IUpdateModel updater)
        {

            var selectedRoles = await _platoRoleStore.GetRoleNamesByUserIdAsync(user.Id);

            return Views(
                View<EditUserRolesViewModel>("Category.Roles.Edit.Content", model =>
                {
                    model.SelectedRoles = selectedRoles;
                    model.HtmlName = HtmlName;
                    return model;
                }).Order(2)
            );

        }

        public override async Task<IViewProviderResult> BuildUpdateAsync(Category user, IUpdateModel updater)
        {

            // Get available role names
            var roleNames = await _platoRoleStore.GetRoleNamesAsync();

            // Convert to list to prevent multiple enumerations below
            var roleNamesList = roleNames.ToList();

            // Build selected roles
            var rolesToAdd = new List<string>();
            foreach (var key in _request.Form.Keys)
            {
                if (key.StartsWith(HtmlName))
                {
                    var values = _request.Form[key];
                    foreach (var value in values)
                    {
                        if (!rolesToAdd.Contains(value))
                        {
                            rolesToAdd.Add(value);
                        }

                    }
                }
            }

            // Update model
            var model = new EditUserRolesViewModel();
            model.SelectedRoles = rolesToAdd;

            if (!await updater.TryUpdateModelAsync(model))
            {
                return await BuildEditAsync(user, updater);
            }

            if (updater.ModelState.IsValid)
            {

                //// Remove roles in two steps to prevent an iteration on a modified collection
                //var rolesToRemove = new List<string>();
                //foreach (var role in await _userManager.GetRolesAsync(user))
                //{
                //    if (!rolesToAdd.Contains(role))
                //    {
                //        rolesToRemove.Add(role);
                //    }
                //}

                //foreach (var role in rolesToRemove)
                //{
                //    await _userManager.RemoveFromRoleAsync(user, role);
                //}

                //// Add new roles
                //foreach (var role in rolesToAdd)
                //{
                //    if (!await _userManager.IsInRoleAsync(user, role))
                //    {
                //        await _userManager.AddToRoleAsync(user, role);
                //    }
                //}

                //var result = await _userManager.UpdateAsync(user);

                //foreach (var error in result.Errors)
                //{
                //    updater.ModelState.AddModelError(string.Empty, error.Description);
                //}

            }

            return await BuildEditAsync(user, updater);

        }

    }
}