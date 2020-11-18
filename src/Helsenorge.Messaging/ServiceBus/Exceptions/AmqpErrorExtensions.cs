using Amqp;
using Amqp.Framing;
using System;
using System.Globalization;
using System.IO;
using System.Net.Sockets;
using System.Text;

namespace Helsenorge.Messaging.ServiceBus.Exceptions
{
    internal static class AmqpErrorExtensions
    {
        public static Exception ToServiceBusException(this Exception exception)
        {
            var stringBuilder = new StringBuilder();
            stringBuilder.AppendFormat(CultureInfo.InvariantCulture, exception.Message);

            var message = stringBuilder.ToString();

            switch (exception)
            {
                case SocketException e:
                    message = stringBuilder.AppendFormat(CultureInfo.InvariantCulture, $" ErrorCode: {e.SocketErrorCode}").ToString();
                    return new ServiceBusCommunicationException(message, exception);

                case IOException _:
                    if (exception.InnerException is SocketException socketException)
                    {
                        message = stringBuilder.AppendFormat(CultureInfo.InvariantCulture, $" ErrorCode: {socketException.SocketErrorCode}").ToString();
                    }
                    return new ServiceBusCommunicationException(message, exception);

                case AmqpException amqpException:
                    return amqpException.Error.ToServiceBusException(amqpException);

                case OperationCanceledException operationCanceledException when operationCanceledException.InnerException is AmqpException amqpException:
                    return amqpException.Error.ToServiceBusException(operationCanceledException);

                case OperationCanceledException _:
                    return new RecoverableServiceBusException(message, exception);

                case TimeoutException _:
                    return new ServiceBusTimeoutException(message, exception);
            }

            return exception;
        }

        public static Exception ToServiceBusException(this Error error, Exception exception)
        {
            return error == null
                ? new UncategorizedServiceBusException("Unknown error.")
                : ToServiceBusException(error.Condition, error.Description, exception);
        }

        private static Exception ToServiceBusException(string condition, string message, Exception exception)
        {
            if (string.Equals(condition, AmqpClientConstants.TimeoutError))
            {
                return new ServiceBusTimeoutException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.NotFound))
            {
                return new MessagingEntityNotFoundException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.NotImplemented))
            {
                return new NotSupportedException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.NotAllowed))
            {
                return new InvalidOperationException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.UnauthorizedAccess) ||
                string.Equals(condition, AmqpClientConstants.AuthorizationFailedError))
            {
                return new UnauthorizedException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.ServerBusyError))
            {
                return new ServerBusyException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.ArgumentError))
            {
                return new ArgumentException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.ArgumentOutOfRangeError))
            {
                return new ArgumentOutOfRangeException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.EntityDisabledError))
            {
                return new MessagingEntityDisabledException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.MessageLockLostError))
            {
                return new MessageLockLostException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.SessionLockLostError))
            {
                return new SessionLockLostException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.ResourceLimitExceeded))
            {
                return new QuotaExceededException(message, exception);
            }

            if (string.Equals(condition, ErrorCode.MessageSizeExceeded))
            {
                return new MessageSizeExceededException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.MessageNotFoundError))
            {
                return new MessageNotFoundException(message, exception);
            }

            if (string.Equals(condition, AmqpClientConstants.SessionCannotBeLockedError))
            {
                return new SessionCannotBeLockedException(message, exception);
            }

            if(string.Equals(condition, ErrorCode.IllegalState))
            {
                return new RecoverableServiceBusException(message, exception);
            }

            return new UncategorizedServiceBusException(message, exception);
        }
    }
}
