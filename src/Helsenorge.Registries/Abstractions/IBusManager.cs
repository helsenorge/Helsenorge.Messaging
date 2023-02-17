/* 
 * Copyright (c) 2022-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;

namespace Helsenorge.Registries.Abstractions
{
    /// <summary>
    /// The IServiceBusManager interface.
    /// </summary>
    public interface IBusManager
    {
        /// <summary>
        /// Returns the subscriptions associated with the authenticated user.
        /// </summary>
        /// <returns></returns>
        Task<IEnumerable<EventSubscription>> GetSubscriptionsAsync();
        
        /// <summary>
        /// Creates a subscription for the authenticated user.
        /// </summary>
        /// <param name="eventSource">Name of the event source</param>
        /// <param name="systemIdentificator">A string representing the system identificator/name for the system that will use the subscription.Valid characters are uppercase, lowercase and underscore.</param>
        /// <param name="eventName">Name of the event type. Can be omitted or an empty string if all event types for a given source are desired.
        /// AddressRegister │ SubscriptionEventName.ArBusEvents.*
        /// Hpr             │ SubscriptionEventName.HprBusEvents.*
        /// Lsr             │ SubscriptionEventName.LsrBusEvents.*
        /// Resh            │ SubscriptionEventName.ReshBusEvents.*
        /// </param>
        /// <returns>returns an instance of a <see cref="EventSubscription"/> containing information about the Subscription.</returns>
        Task<EventSubscription> SubscribeAsync(SubscriptionEventSource eventSource, string systemIdentificator, string eventName = null);

        /// <summary>
        /// Deletes and ends the subscriptions.
        /// </summary>
        /// <param name="queueName">The name of the queue to delete. This information was obtained from the class <see cref="EventSubscription"/>.</param>
        Task UnsubscribeAsync(string queueName);
    }
}
