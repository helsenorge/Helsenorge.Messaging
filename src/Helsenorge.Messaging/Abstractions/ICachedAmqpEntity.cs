/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */
﻿using System.Threading.Tasks;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Interface for items in <see cref="AmqpEntityCache{T}"/>
    /// </summary>
    public interface ICachedAmqpEntity
    {
        /// <summary>
        /// Checks if the item is closed
        /// </summary>
        bool IsClosed { get; }
        /// <summary>
        /// Closes the item
        /// </summary>
        Task CloseAsync();
    }
}
