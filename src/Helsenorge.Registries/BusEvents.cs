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
        /// <summary>Event raised when a communication party is created.</summary>
        public const string CommunicationPartyCreated = nameof(CommunicationPartyCreated);
        /// <summary>Event raised when message broker transport is enabled for a communication.</summary>
        public const string CommunicationPartyTransportEnabled = nameof(CommunicationPartyTransportEnabled);
        /// <summary>Event raised when a communication party is updated.</summary>
        public const string CommunicationPartyUpdated = nameof(CommunicationPartyUpdated);
    }

    /// <summary>
    /// Available events from the "Helsepersonellregisteret" Topic
    /// </summary>
    public static class HprBusEvents
    {
        /// <summary></summary>
        public const string HelsepersonellkategorierSomTillaterTurnusUpdated = nameof(HelsepersonellkategorierSomTillaterTurnusUpdated);
        /// <summary>Event raised when HPR numbers are merged.</summary>
        public const string HprNumbersMerged = nameof(HprNumbersMerged);
        /// <summary>Event raised when a person is created.</summary>
        public const string PersonCreated = nameof(PersonCreated);
        /// <summary>Event raised when a person is updated.</summary>
        public const string PersonUpdated = nameof(PersonUpdated);
        /// <summary>Event raised when a person is deleted.</summary>
        public const string PersonDeleted = nameof(PersonDeleted);
        /// <summary>Event raised when specialist competence is added to a person.</summary>
        public const string SpecialCompetenceAdded = nameof(SpecialCompetenceAdded);
        /// <summary>Event raised when an education institution is created.</summary>
        public const string UtdanningsinstitusjonCreated = nameof(UtdanningsinstitusjonCreated);
        /// <summary>Event raised when an education institution is updated.</summary>
        public const string UtdanningsinstitusjonUpdated = nameof(UtdanningsinstitusjonUpdated);
        /// <summary>Event raised when an education institution is deleted.</summary>
        public const string UtdanningsinstitusjonDeleted = nameof(UtdanningsinstitusjonDeleted);
    }

    /// <summary>
    /// Available events from the "Legestillingsregisteret" Topic
    /// </summary>
    public static class LsrBusEvents
    {
        /// <summary>Event raised when an approval is created.</summary>
        public const string ApprovalCreated = nameof(ApprovalCreated);
        /// <summary>Event raised when an approval is updated.</summary>
        public const string ApprovalUpdated = nameof(ApprovalUpdated);
        /// <summary>Event raised when a distribution is created.</summary>
        public const string DistributionCreated = nameof(DistributionCreated);
        /// <summary>Event raised when a distribution is updated.</summary>
        public const string DistributionUpdated = nameof(DistributionUpdated);
        /// <summary>Event raised when a distribution is deleted.</summary>
        public const string DistributionDeleted = nameof(DistributionDeleted);
        /// <summary>Event raised when an employment is created.</summary>
        public const string EmploymentCreated = nameof(EmploymentCreated);
        /// <summary>Event raised when an employment is updated.</summary>
        public const string EmploymentUpdated = nameof(EmploymentUpdated);
        /// <summary>Event raised when a position is created.</summary>
        public const string PositionCreated = nameof(PositionCreated);
        /// <summary>Event raised when a position is updated.</summary>
        public const string PositionUpdated = nameof(PositionUpdated);
    }

    /// <summary>
    /// Available events from the RESH Topic
    /// </summary>
    public static class ReshBusEvents
    {
        /// <summary>Event raised when a department is created.</summary>
        public const string DepartmentCreated = nameof(DepartmentCreated);
        /// <summary>Event raised when a department is updated.</summary>
        public const string DepartmentUpdated = nameof(DepartmentUpdated);
        /// <summary>Event raised when an organization is created.</summary>
        public const string OrganizationCreated = nameof(OrganizationCreated);
        /// <summary>Event raised when an organization is updated.</summary>
        public const string OrganizationUpdated = nameof(OrganizationUpdated);
        /// <summary>Event raised when a RESH unit is updated.</summary>
        public const string ReshUnitUpdated = nameof(ReshUnitUpdated);
        /// <summary>Event raised when a service is created.</summary>
        public const string ServiceCreated = nameof(ServiceCreated);
        /// <summary>Event raised when a service is updated.</summary>
        public const string ServiceUpdated = nameof(ServiceUpdated);
    }

    /// <summary>
    /// Available events from the Collaboration Protocol Profile/Agreement Topic
    /// </summary>
    public class CppaBusEvents
    {
        /// <summary>Event raised when a CPA is created.</summary>
        public const string CpaCreated = nameof(CpaCreated);
        /// <summary>Event raised when a CPA is terminated.</summary>
        public const string CpaTerminated = nameof(CpaTerminated);
        /// <summary>Event raised when a CPA is updated.</summary>
        public const string CppUpdated = nameof(CppUpdated);
    }
}
