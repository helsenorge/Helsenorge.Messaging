using System;

namespace Helsenorge.Messaging.AdminLib;

public class MissingHeaderException : Exception
{
    public MissingHeaderException(string name)
        : base($"Header with name: '{name}' is missing from the Headers dictionary.")
    {

    }
}
