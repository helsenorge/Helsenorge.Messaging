using System;

namespace Helsenorge.Messaging.AdminLib;

public class SourceAndDestinationIdenticalException : Exception
{
    public SourceAndDestinationIdenticalException(string source, string destination)
        : base($"The source queue and destination queue cannot be identical. Source: '{source}'. Destination: '{destination}'")
    {
    }
}
