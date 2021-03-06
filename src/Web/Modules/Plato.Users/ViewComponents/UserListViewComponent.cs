﻿using System.Collections.Generic;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PlatoCore.Data.Abstractions;
using PlatoCore.Models.Users;
using PlatoCore.Navigation.Abstractions;
using PlatoCore.Security.Abstractions;
using Plato.Users.Services;
using Plato.Users.ViewModels;

namespace Plato.Users.ViewComponents
{
    public class UserListViewComponent : ViewComponent
    {

        private readonly IEnumerable<Filter> _defaultFilters = new List<Filter>()
        {
            new Filter()
            {
                Text = "All",
                Value = FilterBy.All
            },
            new Filter()
            {
                Text = "-",  // represents menu divider
                Permission = Permissions.ViewUnconfirmedUsers
            },
            new Filter()
            {
                Text = "Confirmed",
                Value = FilterBy.Confirmed,
                Permission = Permissions.ViewUnconfirmedUsers
            },
            new Filter()
            {
                Text = "Unconfirmed",
                Value = FilterBy.Unconfirmed,
                Permission = Permissions.ViewUnconfirmedUsers
            },
            new Filter()
            {
                Text = "-" // represents menu divider
            },
            new Filter()
            {
                Text = "Verified",
                Value = FilterBy.Verified
            },
            new Filter()
            {
                Text = "Staff",
                Value = FilterBy.Staff
            },
            new Filter()
            {
                Text = "Spam",
                Value = FilterBy.Spam,
                Permission = Permissions.ViewSpamUsers
            },
            new Filter()
            {
                Text = "Banned",
                Value = FilterBy.Banned,
                Permission = Permissions.ViewBannedUsers
            }
        };
        
        private readonly IEnumerable<SortColumn> _defaultSortColumns = new List<SortColumn>()
        {
            new SortColumn()
            {
                Text = "Last Active",
                Value = SortBy.LastLoginDate
            },
            new SortColumn()
            {
                Text = "Reputation",
                Value =  SortBy.Reputation
            },
            new SortColumn()
            {
                Text = "Rank",
                Value = SortBy.Rank
            },
            //new SortColumn()
            //{
            //    Text = "Time Spent",
            //    Value =  SortBy.Minutes
            //},
            new SortColumn()
            {
                Text = "Visits",
                Value = SortBy.Visits
            },
            new SortColumn()
            {
                Text = "Created",
                Value = SortBy.CreatedDate
            },
            new SortColumn()
            {
                Text = "Modified",
                Value = SortBy.ModifiedDate
            }
        };

        private readonly IEnumerable<SortOrder> _defaultSortOrder = new List<SortOrder>()
        {
            new SortOrder()
            {
                Text = "Descending",
                Value = OrderBy.Desc
            },
            new SortOrder()
            {
                Text = "Ascending",
                Value = OrderBy.Asc
            },
        };

        private readonly IAuthorizationService _authorizationService;
        private readonly IUserService<User> _userService;

        public UserListViewComponent(
            IAuthorizationService authorizationService,
            IUserService<User> userService)
        {
            _authorizationService = authorizationService;
            _userService = userService;
        }

        public async Task<IViewComponentResult> InvokeAsync(UserIndexOptions options, PagerOptions pager)
        {

            if (options == null)
            {
                options = new UserIndexOptions();
            }
            
            if (pager == null)
            {
                pager = new PagerOptions();
            }
            
            return View(await GetIndexViewModel(options, pager));
        }

        private async Task<UserIndexViewModel> GetIndexViewModel(UserIndexOptions options, PagerOptions pager)
        {

            var results = await _userService
                .ConfigureQuery(async q =>
                {

                    // Hide unconfirmed?
                    if (!await _authorizationService.AuthorizeAsync(HttpContext.User,
                        Permissions.ViewUnconfirmedUsers))
                    {
                        q.HideUnconfirmed.True();
                    }

                    // Hide SPAM?
                    if (!await _authorizationService.AuthorizeAsync(HttpContext.User,
                        Permissions.ViewSpamUsers))
                    {
                        q.HideSpam.True();
                    }

                    // Hide Banned?
                    if (!await _authorizationService.AuthorizeAsync(HttpContext.User,
                        Permissions.ViewBannedUsers))
                    {
                        q.HideBanned.True();
                    }

                })
                .GetResultsAsync(options, pager);

            // Set total on pager
            pager.SetTotal(results?.Total ?? 0);
            
            // Return view model
            return new UserIndexViewModel
            {
                SortColumns = _defaultSortColumns,
                SortOrder = _defaultSortOrder,
                Filters = _defaultFilters,
                Results = results,
                Options = options,
                Pager = pager
            };

        }

    }

}

