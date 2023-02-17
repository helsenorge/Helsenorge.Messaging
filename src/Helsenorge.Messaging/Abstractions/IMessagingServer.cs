/* 
 * Copyright (c) 2020-2023, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using Helsenorge.Messaging.Amqp.Receivers;
using System;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace Helsenorge.Messaging.Abstractions
{
    /// <summary>
    /// Main interface for receiving messages
    /// </summary>
    public interface IMessagingServer
    {
        /// <summary>
        /// Start message processing
        /// </summary>
        Task StartAsync();
        /// <summary>
        /// Terminate message processing
        /// </summary>
        Task StopAsync();

        /// <summary>
        /// Terminate message processing
        /// </summary>
        /// <param name="timeout">The amount of time we wait for things to shut down</param>
        Task StopAsync(TimeSpan timeout = default);

        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedStartingCallback(Action<MessageListener, IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedStartingCallbackAsync(Func<MessageListener, IncomingMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCallbackAsync(Func<IncomingMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCompletedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterAsynchronousMessageReceivedCompletedCallbackAsync(Func<IncomingMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedStartingCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called as we start processing a message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedStartingCallbackAsync(Func<IncomingMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCallback(Func<IncomingMessage, XDocument> action);
        /// <summary>
        /// Registers a delegate that should be called when we have enough information to process the message. This is where the main processing logic hooks in.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCallbackAsync(Func<IncomingMessage, Task<XDocument>> action);

        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCompletedCallback(Action<IncomingMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we are finished processing the message.
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterSynchronousMessageReceivedCompletedCallbackAsync(Func<IncomingMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we receive an error message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterErrorMessageReceivedCallback(Action<IAmqpMessage> action);
        /// <summary>
        /// Registers a delegate that should be called when we receive an error message
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterErrorMessageReceivedCallbackAsync(Func<IAmqpMessage, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we have an handled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterHandledExceptionCallback(Action<IAmqpMessage, Exception> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an handled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterHandledExceptionCallbackAsync(Func<IAmqpMessage, Exception, Task> action);

        /// <summary>
        /// Registers a delegate that should be called when we have an unhandled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterUnhandledExceptionCallback(Action<IAmqpMessage, Exception> action);
        /// <summary>
        /// Registers a delegate that should be called when we have an unhandled exception
        /// </summary>
        /// <param name="action">The delegate that should be called</param>
        void RegisterUnhandledExceptionCallbackAsync(Func<IAmqpMessage, Exception, Task> action);
    }
}
