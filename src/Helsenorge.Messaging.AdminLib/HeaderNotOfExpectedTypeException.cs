using System;

namespace Helsenorge.Messaging.AdminLib;

public class HeaderNotOfExpectedTypeException : Exception
{
    public HeaderNotOfExpectedTypeException(string name, Type type)
        : base($"Expected header: '{name}' to of type '{type.FullName}'")
    {
        throw new NotImplementedException();
    }
}
