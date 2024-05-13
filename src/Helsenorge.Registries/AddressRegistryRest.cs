/*
 * Copyright (c) 2020-2024, Norsk Helsenett SF and contributors
 * See the file CONTRIBUTORS for details.
 *
 * This file is licensed under the MIT license
 * available at https://raw.githubusercontent.com/helsenorge/Helsenorge.Messaging/master/LICENSE
 */

using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Helsenorge.Registries.Abstractions;
using Helsenorge.Registries.Utilities;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Logging;
using CertificateDetails = Helsenorge.Registries.Abstractions.CertificateDetails;

namespace Helsenorge.Registries;

public class AddressRegistryRest : IAddressRegistry
{
    private readonly IDistributedCache _cache;
    private readonly RestServiceInvoker _invoker;
    private readonly ILogger _logger;
    private readonly AddressRegistryRestSettings _settings;

    public AddressRegistryRest(
        AddressRegistryRestSettings settings,
        IDistributedCache cache,
        ILogger logger,
        ICertificateValidator certificateValidator = null)
    {
        if (settings == null) throw new ArgumentNullException(nameof(settings));
        if (cache == null) throw new ArgumentNullException(nameof(cache));
        if (logger == null) throw new ArgumentNullException(nameof(logger));

        _settings = settings;
        _cache = cache;
        _logger = logger;
    }

    public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId)
    {
        throw new NotImplementedException();
    }

    public Task<CommunicationPartyDetails> FindCommunicationPartyDetailsAsync(int herId, bool forceUpdate)
    {
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId)
    {
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForEncryptionAsync(int herId, bool forceUpdate)
    {
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId)
    {
        throw new NotImplementedException();
    }

    public Task<CertificateDetails> GetCertificateDetailsForValidatingSignatureAsync(int herId, bool forceUpdate)
    {
        throw new NotImplementedException();
    }

    public Task PingAsync()
    {
        throw new NotImplementedException();
    }

    public Task<IEnumerable<CommunicationPartyDetails>> SearchByIdAsync(string id, bool forceUpdate = false)
    {
        throw new NotImplementedException();
    }

    public Task<OrganizationDetails> GetOrganizationDetailsAsync(int herId, bool forceUpdate = false)
    {
        throw new NotImplementedException();
    }
}