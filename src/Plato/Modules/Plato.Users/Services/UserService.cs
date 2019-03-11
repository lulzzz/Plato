﻿using System.Threading.Tasks;
using Plato.Internal.Data.Abstractions;
using Plato.Internal.Models.Users;
using Plato.Internal.Navigation.Abstractions;
using Plato.Internal.Stores.Abstractions.Users;
using Plato.Internal.Stores.Users;
using Plato.Users.ViewModels;

namespace Plato.Users.Services
{
    
    public class UserService : IUserService
    {

        private readonly IPlatoUserStore<User> _platoUserStore;

        public UserService(IPlatoUserStore<User> platoUserStore)
        {
            _platoUserStore = platoUserStore;
        }

        public async Task<IPagedResults<User>> GetResultsAsync(
            UserIndexOptions options,
            PagerOptions pager)
        {
            return await _platoUserStore.QueryAsync()
                .Take(pager.Page, pager.Size)
                .Select<UserQueryParams>(q =>
                {

                    switch (options.Filter)
                    {
                        case FilterBy.Confirmed:
                            q.ShowConfirmed.True();
                            break;
                        case FilterBy.Banned:
                            q.ShowBanned.True();
                            break;
                        case FilterBy.Locked:
                            q.ShowLocked.True();
                            break;
                        case FilterBy.Spam:
                            q.ShowSpam.True();
                            break;
                        case FilterBy.PossibleSpam:
                            q.HideConfirmed.True();
                            break;
                        default:
                            break;
                    }

                    if (!string.IsNullOrEmpty(options.Search))
                    {
                        q.Keywords.Like(options.Search);
                    }

                })
                .OrderBy(options.Sort.ToString(), options.Order)
                .ToList();

        }

    }

}
