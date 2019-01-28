﻿using System;
using System.Threading.Tasks;
using Plato.Discuss.Models;
using Plato.Entities.Stores;
using Plato.Internal.Messaging.Abstractions;
using Plato.Internal.Reputations.Abstractions;

namespace Plato.Discuss.Star.Subscribers
{
    public class StarSubscriber : IBrokerSubscriber
    {

        private readonly IEntityStore<Topic> _entityStore;
        private readonly IBroker _broker;
        private readonly IUserReputationAwarder _reputationAwarder;

        public StarSubscriber(
            IBroker broker,
            IUserReputationAwarder reputationAwarder,
            IEntityStore<Topic> entityStore)
        {
            _broker = broker;
            _reputationAwarder = reputationAwarder;
            _entityStore = entityStore;
        }

        public void Subscribe()
        {
 
            _broker.Sub<Stars.Models.Star>(new MessageOptions()
            {
                Key = "FollowCreated"
            }, async message => await FollowCreated(message.What));
            
            _broker.Sub<Stars.Models.Star>(new MessageOptions()
            {
                Key = "FollowDeleted"
            }, async message => await FollowDeleted(message.What));

        }

        public void Unsubscribe()
        {
            // Add a reputation for new follows
            _broker.Unsub<Stars.Models.Star>(new MessageOptions()
            {
                Key = "FollowCreated"
            }, async message => await FollowCreated(message.What));

            _broker.Unsub<Stars.Models.Star>(new MessageOptions()
            {
                Key = "FollowDeleted"
            }, async message => await FollowDeleted(message.What));

        }

        private async Task<Stars.Models.Star> FollowCreated(Stars.Models.Star follow)
        {

            if (follow == null)
            {
                return null;
            }

            // Is this a tag follow?
            if (!follow.Name.Equals(StarTypes.Topic.Name, StringComparison.OrdinalIgnoreCase))
            {
                return follow;
            }
            
            // Ensure the topic we are following still exists
            var existingTopic = await _entityStore.GetByIdAsync(follow.ThingId);
            if (existingTopic == null)
            {
                return follow;
            }

            // Update total follows
            existingTopic.TotalFollows = existingTopic.TotalFollows + 1;

            // Persist changes
            var updatedTopic = await _entityStore.UpdateAsync(existingTopic);
            if (updatedTopic != null)
            {
                // Award reputation for following topic
                //await _reputationAwarder.AwardAsync(Reputations.NewFollow, follow.CreatedUserId);
            }
            
            return follow;

        }

        private async Task<Stars.Models.Star> FollowDeleted(Stars.Models.Star follow)
        {

            if (follow == null)
            {
                return null;
            }

            // Is this a topic follow?
            if (!follow.Name.Equals(StarTypes.Topic.Name, StringComparison.OrdinalIgnoreCase))
            {
                return follow;
            }

            // Ensure the topic we are following still exists
            var existingTopic = await _entityStore.GetByIdAsync(follow.ThingId);
            if (existingTopic == null)
            {
                return follow;
            }

            // Update total follows
            existingTopic.TotalFollows = existingTopic.TotalFollows - 1;
        
            // Ensure we don't go negative
            if (existingTopic.TotalFollows < 0)
            {
                existingTopic.TotalFollows = 0;
            }
            
            // Persist changes
            var updatedTopic = await _entityStore.UpdateAsync(existingTopic);
            if (updatedTopic != null)
            {
                // Revoke reputation for following tag
                //await _reputationAwarder.RevokeAsync(Reputations.NewFollow, follow.CreatedUserId);
            }
            
            return follow;

        }

    }

}
