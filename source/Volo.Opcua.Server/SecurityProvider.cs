using LibUA;
using System;
using System.IO;
using System.Security.Cryptography;
using System.Security.Cryptography.X509Certificates;
using Volo.Opcua.Server.Shared;

namespace Volo.Opcua.Server
{
    public class SecurityProvider
    {
        private readonly AppSettings _appSettings;

        public X509Certificate2 Cert { get; private set; }
        public RSACryptoServiceProvider Key { get; private set; }

        public SecurityProvider(AppSettings appSettings)
        {
            _appSettings = appSettings;
        }

        public void LoadCertificateAndPrivateKey()
        {
            try
            {
                Cert = new X509Certificate2(_appSettings.Certificate);
                Key = new RSACryptoServiceProvider();

                var rsaPrivParams = UASecurity.ImportRSAPrivateKey(File.ReadAllText(_appSettings.PrivateKey));
                Key.ImportParameters(rsaPrivParams);
            }
            catch
            {
                var dn = new X500DistinguishedName($"CN={_appSettings.CommonName};OU={_appSettings.OrganizationalUnit}", X500DistinguishedNameFlags.UseSemicolons);
                SubjectAlternativeNameBuilder sanBuilder = new SubjectAlternativeNameBuilder();
                sanBuilder.AddUri(new Uri($"urn:{_appSettings.ApplicationUri}"));

                using (RSA rsa = RSA.Create(2048))
                {
                    var request = new CertificateRequest(dn, rsa, HashAlgorithmName.SHA256, RSASignaturePadding.Pkcs1);

                    request.CertificateExtensions.Add(sanBuilder.Build());

                    var selfSignedCert = request.CreateSelfSigned(new DateTimeOffset(DateTime.UtcNow.AddDays(-1)), new DateTimeOffset(DateTime.UtcNow.AddDays(3650)));

                    Cert = new X509Certificate2(selfSignedCert.Export(X509ContentType.Pfx, ""), "", X509KeyStorageFlags.DefaultKeySet);

                    var certPrivateParams = rsa.ExportParameters(true);
                    File.WriteAllText(_appSettings.Certificate, UASecurity.ExportPEM(Cert));
                    File.WriteAllText(_appSettings.PrivateKey, UASecurity.ExportRSAPrivateKey(certPrivateParams));

                    Key = new RSACryptoServiceProvider();
                    Key.ImportParameters(certPrivateParams);
                }
            }
        }
    }
}