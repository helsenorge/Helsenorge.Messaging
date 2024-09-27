/* 
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 * 
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
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

    /// <summary>
    /// Returns true if CollaborationProtocolProfile.Name equals 'DummyCollaborationProtocolProfile' or 'MessageFunctionExceptionProfile'.
    /// </summary>
    /// <param name="cpp"></param>
    /// <returns></returns>
    public static bool IsDummyProfile(CollaborationProtocolProfile cpp) => cpp?.Name == DummyPartyName || cpp?.Name == MessageFunctionExceptionProfileName;

    /// <summary>
    /// Creates a "dummy" <see cref="CollaborationProtocolProfile"/>.
    /// </summary>
    /// <param name="addressRegistry">An instance of <see cref="IAddressRegistry"/>.</param>
    /// <param name="logger">An instance of <see cref="ILogger"/>.</param>
    /// <param name="herId">The HER-id to create a "dummy" <see cref="CollaborationProtocolProfile"/></param>
    /// <param name="messageFunction"></param>
    /// <param name="collaborationProtocolRegistry"></param>
    /// <returns></returns>
    public static async Task<CollaborationProtocolProfile> CreateAsync(
        IAddressRegistry addressRegistry,
        ILogger logger,
        int herId,
        string messageFunction,
        ICollaborationProtocolRegistry collaborationProtocolRegistry = null,
        ICertificateValidator certificateValidator = null)
    {
        var profile = await addressRegistry.FindCommunicationPartyDetailsAsync(herId).ConfigureAwait(false);

        if (profile == null)
        {
            logger.LogWarning($"Could not get communication party details for HerId {herId}");
            return null;
        }
        
        var deliveryChannel = profile.AsynchronousQueueName;

        if (collaborationProtocolRegistry != null && certificateValidator != null)
        {
            var collaborationProtocolProfile = await collaborationProtocolRegistry.FindProtocolForCounterpartyAsync(herId).ConfigureAwait(false);
            
            if (collaborationProtocolProfile == null)
            {
                logger.LogWarning($"Could not get collaborationProtocolProfile details for HerId {herId}");
                return null;
            }
            if (collaborationProtocolProfile.Roles == null || collaborationProtocolProfile.Roles.Count == 0)
            {
                logger.LogWarning($"Could not get collaborationProtocolProfileRoles for for HerId {herId}");
                return null;
            }
            var encryptionCertificate = new CertificateDetails
            {
                HerId = collaborationProtocolProfile.HerId,
                Certificate = collaborationProtocolProfile.EncryptionCertificate
            };
            var signatureCertificate = new CertificateDetails
            {
                HerId = collaborationProtocolProfile.HerId,
                Certificate = collaborationProtocolProfile.SignatureCertificate
            };

            ValidateCertificate(herId, certificateValidator, encryptionCertificate, X509KeyUsageFlags.KeyEncipherment);
            ValidateCertificate(herId, certificateValidator, signatureCertificate, X509KeyUsageFlags.NonRepudiation);

            return CreateDummyCollaborationProtocolProfile(herId,
                encryptionCertificate,
                signatureCertificate,
                deliveryChannel,
                messageFunction);
        }

        return CreateDummyCollaborationProtocolProfile(herId,
            await addressRegistry.GetCertificateDetailsForEncryptionAsync(herId).ConfigureAwait(false),
            await addressRegistry.GetCertificateDetailsForValidatingSignatureAsync(herId).ConfigureAwait(false),
            deliveryChannel,
            messageFunction);
    }

    private static void ValidateCertificate(int herId, ICertificateValidator certificateValidator, CertificateDetails encryptionCertificate, X509KeyUsageFlags usage)
    {
        if (certificateValidator != null && encryptionCertificate?.Certificate != null)
        {
            var error = certificateValidator.Validate(encryptionCertificate.Certificate, usage);
            if (error != CertificateErrors.None)
            {
                throw new CouldNotVerifyCertificateException($"Could not verify HerId: {herId} certificate", herId);
            }
        }
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
