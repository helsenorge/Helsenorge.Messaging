/* 
 * Copyright (c) 2020-2022, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Microsoft.Extensions.Logging;

namespace Helsenorge.Registries;

/// <summary>
/// This is designed so it can be used as a singleton
/// </summary>
public static class DummyCollaborationProtocolProfileFactory
{

    /// <summary>
    /// A constant that is used to indicate a Dummy Collaboration Protocol Profile (CPP) in case the Communication Party completely lacks a CPP.
    /// </summary>
    /// <remarks>
    /// This will be removed in a future version without warning, since lacking a CPP is not considered good or acceptable practice
    /// </remarks>
    private const string DummyPartyName = "DummyCollaborationProtocolProfile";

    private const string MessageFunctionExceptionProfileName = "MessageFunctionExceptionProfile";

    public static bool IsDummyProfile(CollaborationProtocolProfile cpp) => cpp?.Name == DummyPartyName || cpp?.Name == MessageFunctionExceptionProfileName;

    public static async Task<CollaborationProtocolProfile> CreateAsync(IAddressRegistry addressRegistry, ILogger logger, int herId, string messageFunction)
    {
        var communicationParty = await addressRegistry.FindCommunicationPartyDetailsAsync(logger, herId).ConfigureAwait(false);
        if(communicationParty == null)
        {
            logger.LogWarning($"Could not get communication party details for HerId {herId}");
            return null;
        }

        var deliveryChannel = communicationParty.AsynchronousQueueName;
        return CreateDummyCollaborationProtocolProfile(herId,
            await addressRegistry.GetCertificateDetailsForEncryptionAsync(logger, herId).ConfigureAwait(false),
            await addressRegistry.GetCertificateDetailsForValidatingSignatureAsync(logger, herId).ConfigureAwait(false),
            deliveryChannel,
            messageFunction);
    }

    private static CollaborationProtocolProfile CreateDummyCollaborationProtocolProfile(int herId, CertificateDetails encryptionCertificate, CertificateDetails signatureCertificate, string deliveryChannel, string messageFunction)
    {
        return new CollaborationProtocolProfile
        {
            Roles = new List<CollaborationProtocolRole>
            {
                new CollaborationProtocolRole
                {
                    ReceiveMessages = new List<CollaborationProtocolMessage>
                    {
                        new CollaborationProtocolMessage
                        {
                            Name = messageFunction ?? "APPREC",
                            Action = messageFunction ?? "APPREC",
                            DeliveryProtocol = DeliveryProtocol.Amqp,
                            DeliveryChannel = deliveryChannel
                        }
                    },
                    SendMessages = new List<CollaborationProtocolMessage>
                    {
                        new CollaborationProtocolMessage
                        {
                            Name = messageFunction ?? "APPREC",
                            Action = messageFunction ?? "APPREC",
                            DeliveryProtocol = DeliveryProtocol.Amqp,
                            DeliveryChannel = deliveryChannel
                        }
                    }
                }
            },
            HerId = herId,
            Name = messageFunction != null ? MessageFunctionExceptionProfileName : DummyPartyName,
            EncryptionCertificate = encryptionCertificate?.Certificate,
            SignatureCertificate = signatureCertificate?.Certificate
        };
    }
}
