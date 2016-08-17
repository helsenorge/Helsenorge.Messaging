using System;
using System.Xml.Linq;
using Helsenorge.Messaging.Security;
using Helsenorge.Messaging.Tests.Mocks;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace Helsenorge.Messaging.Tests.Security
{
	[TestClass]
	[DeploymentItem(@"Files", @"Files")]
	public class SignThenEncryptMessageProtectionTests
	{
		private XDocument _content;
		private SignThenEncryptMessageProtection _protection;
		[TestInitialize]
		public void Setup()
		{
			_content = new XDocument(new XElement("SomeDummyXml"));
			_protection = new SignThenEncryptMessageProtection();
		}

		[TestMethod]
		[TestCategory("X509Chain")]
		public void Protect_And_Unprotect_OK()
		{
			var stream = _protection.Protect(
				_content, 
				TestCertificates.HelsenorgePublicEncryption,
				TestCertificates.CounterpartyPrivateSigntature);

			var result = _protection.Unprotect(
				stream, 
				TestCertificates.HelsenorgePrivateEncryption,
				TestCertificates.CounterpartyPublicSignature);

			Assert.AreEqual(_content.ToString(), result.ToString());
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Protect_Data_ArgumentNullException()
		{
			_protection.Protect(null, TestCertificates.HelsenorgePublicEncryption, TestCertificates.CounterpartyPrivateSigntature);
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Protect_Encryption_ArgumentNullException()
		{
			_protection.Protect(_content, null, TestCertificates.CounterpartyPrivateSigntature);
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		public void Protect_Signature_ArgumentNullException()
		{
			_protection.Protect(_content, TestCertificates.HelsenorgePublicEncryption, null);
		}

		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		[TestCategory("X509Chain")]
		public void Unprotect_Data_ArgumentNullException()
		{
			var stream = _protection.Protect(
				_content,
				TestCertificates.HelsenorgePublicEncryption,
				TestCertificates.CounterpartyPrivateSigntature);

			_protection.Unprotect(null, TestCertificates.HelsenorgePrivateEncryption, TestCertificates.CounterpartyPublicSignature);
		}
		[TestMethod]
		[ExpectedException(typeof(ArgumentNullException))]
		[TestCategory("X509Chain")]
		public void Unprotect_Encryption_ArgumentNullException()
		{
			var stream = _protection.Protect(
				_content,
				TestCertificates.HelsenorgePublicEncryption,
				TestCertificates.CounterpartyPrivateSigntature);

			_protection.Unprotect(stream, null, TestCertificates.CounterpartyPublicSignature);
		}
		[TestMethod]
		[TestCategory("X509Chain")]
		public void Unprotect_Signature_ArgumentNullException()
		{
			var stream = _protection.Protect(
				_content,
				TestCertificates.HelsenorgePublicEncryption,
				TestCertificates.CounterpartyPrivateSigntature);

			var result = _protection.Unprotect(stream, TestCertificates.HelsenorgePrivateEncryption, null);
			Assert.AreEqual(_content.ToString(), result.ToString());
		}
	}
}
