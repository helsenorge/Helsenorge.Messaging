/* 
 * Copyright (c) 2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

namespace Helsenorge.Registries
{
    // These constants were fetched from:
    // https://github.com/NorskHelsenett/NHNDtoContracts/blob/master/src/NHN.DtoContracts/NHN.DtoContracts/ServiceBus/Data/SubscriptionEventName.cs

    /// <summary>
    /// Available events from the AddressRegistry Topic
    /// </summary>
    public static class ArBusEvents
    {
        public const string CommunicationPartyCreated = nameof(CommunicationPartyCreated);
        public const string CommunicationPartyTransportEnabled = nameof(CommunicationPartyTransportEnabled);
        public const string CommunicationPartyUpdated = nameof(CommunicationPartyUpdated);
    }

    /// <summary>
    /// Available events from the "Helsepersonellregisteret" Topic
    /// </summary>
    public static class HprBusEvents
    {
        public const string HelsepersonellkategorierSomTillaterTurnusUpdated = nameof(HelsepersonellkategorierSomTillaterTurnusUpdated);
        public const string HprNumbersMerged = nameof(HprNumbersMerged);
        public const string PersonCreated = nameof(PersonCreated);
        public const string PersonUpdated = nameof(PersonUpdated);
        public const string PersonDeleted = nameof(PersonDeleted);
        public const string SpecialCompetenceAdded = nameof(SpecialCompetenceAdded);
        public const string UtdanningsinstitusjonCreated = nameof(UtdanningsinstitusjonCreated);
        public const string UtdanningsinstitusjonUpdated = nameof(UtdanningsinstitusjonUpdated);
        public const string UtdanningsinstitusjonDeleted = nameof(UtdanningsinstitusjonDeleted);
    }

    /// <summary>
    /// Available events from the "Legestillingsregisteret" Topic
    /// </summary>
    public static class LsrBusEvents
    {
        public const string ApprovalCreated = nameof(ApprovalCreated);
        public const string ApprovalUpdated = nameof(ApprovalUpdated);
        public const string DistributionCreated = nameof(DistributionCreated);
        public const string DistributionUpdated = nameof(DistributionUpdated);
        public const string DistributionDeleted = nameof(DistributionDeleted);
        public const string EmploymentCreated = nameof(EmploymentCreated);
        public const string EmploymentUpdated = nameof(EmploymentUpdated);
        public const string PositionCreated = nameof(PositionCreated);
        public const string PositionUpdated = nameof(PositionUpdated);
    }

    /// <summary>
    /// Available events from the RESH Topic
    /// </summary>
    public static class ReshBusEvents
    {
        public const string DepartmentCreated = nameof(DepartmentCreated);
        public const string DepartmentUpdated = nameof(DepartmentUpdated);
        public const string OrganizationCreated = nameof(OrganizationCreated);
        public const string OrganizationUpdated = nameof(OrganizationUpdated);
        public const string ReshUnitUpdated = nameof(ReshUnitUpdated);
        public const string ServiceCreated = nameof(ServiceCreated);
        public const string ServiceUpdated = nameof(ServiceUpdated);
    }

    /// <summary>
    /// Available events from the Collaboration Protocol Profile/Agreement Topic
    /// </summary>
    public class CppaBusEvents
    {
        public const string CpaCreated = nameof(CpaCreated);
        public const string CpaTerminated = nameof(CpaTerminated);
        public const string CppUpdated = nameof(CppUpdated);
    }
}
