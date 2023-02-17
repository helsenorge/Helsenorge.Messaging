﻿/* 
 * Copyright (c) 2020, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Amqp.Types;

namespace Helsenorge.Messaging.Amqp
{
    internal class AmqpClientConstants
    {
        public static readonly Symbol TimeoutError = AmqpConstants.Vendor + ":timeout";
        public static readonly Symbol AuthorizationFailedError = AmqpConstants.Vendor + ":auth-failed";
        public static readonly Symbol MessageLockLostError = AmqpConstants.Vendor + ":message-lock-lost";
        public static readonly Symbol SessionLockLostError = AmqpConstants.Vendor + ":session-lock-lost";
        public static readonly Symbol SessionCannotBeLockedError = AmqpConstants.Vendor + ":session-cannot-be-locked";
        public static readonly Symbol ServerBusyError = AmqpConstants.Vendor + ":server-busy";
        public static readonly Symbol ArgumentError = AmqpConstants.Vendor + ":argument-error";
        public static readonly Symbol ArgumentOutOfRangeError = AmqpConstants.Vendor + ":argument-out-of-range";
        public static readonly Symbol EntityDisabledError = AmqpConstants.Vendor + ":entity-disabled";
        public static readonly Symbol MessageNotFoundError = AmqpConstants.Vendor + ":message-not-found";
    }
}
