// See https://aka.ms/new-console-template for more information

using Helsenorge.Registries;
using Helsenorge.Registries.Configuration;
using Microsoft.Extensions.Logging.Abstractions;

namespace SearchByIdAndGetOrganizationDetailsExample;

internal static class Program
{
    private const string Username = "<username>";
    private const string Password = "<password>";
    private static async Task Main()
    {
        var cache = DistributedCacheFactory.Create();
        var logger = new NullLoggerFactory().CreateLogger("SearchByIdAndGetOrganizationDetailsExample");
        var settings = new AddressRegistrySettings
        {
            WcfConfiguration = new WcfConfiguration
            {
                Address = "https://ws-web.test.nhn.no/v1/AR/Basic",
                MaxBufferSize = 2147483647,
                MaxBufferPoolSize = 2147483647,
                MaxReceivedMessageSize = 2147483647,
                UserName = Username,
                Password = Password,
            },
            CachingInterval = TimeSpan.FromMinutes(60),
        };
        var addressRegistry = new AddressRegistry(settings, cache, logger);

        var communicationPartyDetailsList = await addressRegistry.SearchByIdAsync("912039374");
        var communicationPartyDetails = communicationPartyDetailsList.FirstOrDefault();
        if (communicationPartyDetails == null)
            goto exit;

        var organizationDetails = await addressRegistry.GetOrganizationDetailsAsync(communicationPartyDetails.HerId);

        Console.WriteLine($"Organization OrganizationNumber: {organizationDetails.OrganizationNumber}");
        Console.WriteLine($"Organization HER-Id: {organizationDetails.HerId}");
        Console.WriteLine($"Organization Name: {organizationDetails.Name}");
        Console.WriteLine($"Organization BusinessType - OID: {organizationDetails.BusinessType.OID}, Value: {organizationDetails.BusinessType.Value}, Text: {organizationDetails.BusinessType.Text}");
        Console.WriteLine($"Organization Active: {organizationDetails.Active}");
        Console.WriteLine($"Organization Parent OrganizationNumber: {organizationDetails.ParentOrganizationNumber}");
        Console.WriteLine($"Organization Service Type: {organizationDetails.Type}");
        Console.WriteLine($"Organization IsValidCommunicationParty: {organizationDetails.IsValidCommunicationParty}");

        Console.WriteLine($"Organization IndustryCodes:");
        foreach (var industryCode in organizationDetails.IndustryCodes)
            Console.WriteLine($"Organization IndustryCode - OID: {industryCode.OID}, Value: {industryCode.Value}, Text: {industryCode.Text}");

        Console.WriteLine("Services:");
        foreach (var service in organizationDetails.Services)
        {
            Console.WriteLine($"Service HER-Id: {service.HerId}");
            Console.WriteLine($"Service Name: {service.Name}");
            Console.WriteLine($"Service ParentName: {service.ParentName}");
            Console.WriteLine($"Service Code - OID: {service.Code.OID}, Value: {service.Code.Value}, Text: {service.Code.Text}");
            Console.WriteLine($"Service Description: {service.Description}");
            Console.WriteLine($"Service LocationDescription: {service.LocationDescription}");
            Console.WriteLine($"Service Parent OrganizationNumber: {service.ParentOrganizationNumber}");
            Console.WriteLine($"Service Type: {service.Type}");
            Console.WriteLine($"Service IsValidCommunicationParty: {service.IsValidCommunicationParty}");
        }

        exit:;
    }
}
