using System;
using System.Security.Cryptography.X509Certificates;
using System.Text;
using Helsenorge.Registries.Abstractions;

namespace Helsenorge.Registries
{
	/// <summary>
	/// Default implementation of <see cref="ICertificateValidator"/>
	/// </summary>
	public class CertificateValidator : ICertificateValidator
	{
		/// <summary>
		/// Validates the provided certificate
		/// </summary>
		/// <param name="certificate">The certificate to validate</param>
		/// <param name="usage">The type of usage the certificate is specified for</param>
		/// <returns>A bitcoded status indicating if the certificate is valid or not</returns>
		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Reliability", "CA2000:Dispose objects before losing scope")]
		public CertificateErrors Validate(X509Certificate2 certificate, X509KeyUsageFlags usage)
		{
			if (certificate == null) return CertificateErrors.Missing;

			var result = CertificateErrors.None;

			if (DateTime.Now < certificate.NotBefore)
			{
				result |= CertificateErrors.StartDate;
			}
			if (DateTime.Now > certificate.NotAfter)
			{
				result |= CertificateErrors.EndDate;
			}

			foreach (var extension in certificate.Extensions)
			{
				switch (extension.Oid.Value)
				{
					case "2.5.29.15": // Key usage
						var usageExtension = (X509KeyUsageExtension)extension;
						if ((usageExtension.KeyUsages & usage) != usage)
						{
							result |= CertificateErrors.Usage;
						}
						break;
				}
			}

			var chain = new X509Chain
			{
				ChainPolicy =
				{
					RevocationMode = X509RevocationMode.Online,
					RevocationFlag = X509RevocationFlag.EntireChain,
					UrlRetrievalTimeout = TimeSpan.FromSeconds(30),
					VerificationTime = DateTime.Now,
				}
			};

			using (chain)
			{
				if (chain.Build(certificate)) return result;
				var sb = new StringBuilder();

				foreach (var status in chain.ChainStatus)
				{
					// ReSharper disable once SwitchStatementMissingSomeCases
					switch (status.Status)
					{
						case X509ChainStatusFlags.OfflineRevocation:
						case X509ChainStatusFlags.RevocationStatusUnknown:
							result |= CertificateErrors.RevokedUnknown;
							break;
						case X509ChainStatusFlags.Revoked:
							result |= CertificateErrors.Revoked;
							break;
					}
					sb.AppendLine(status.StatusInformation);
				}
				return result;
			}
		}
	}
}
