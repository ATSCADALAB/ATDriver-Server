using System.IO;
using System.Reflection;
using System.Security.Cryptography.X509Certificates;

namespace ATDriver_Server
{
    public class CertificateEngine
    {
        private const string CertificatePath = @"C:\Program Files\ATPro\ATSCADA Certificate";

        private const string ServerCertificate = @"ATSCADAServer.pfx";

        private const string ClientCertificate = @"ATSCADAClient.pfx";

        private const string Password = "ATSCADA";

        public X509Certificate2 GetServerCertifcate()
        {
            try
            {
                return GetServerCertifcate(ServerCertificate, Password);
            }
            catch
            {
                return null;
            }            
        }

        public X509Certificate2 GetClientCertifcate()
        {
            try
            {
                return GetServerCertifcate(ClientCertificate, Password);
            }
            catch
            {
                return null;
            }
        }

        private X509Certificate2 GetServerCertifcate(string fileName, string password)
        {
            var locationPath = Path.Combine(Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location), fileName);
            var storagePath = Path.Combine(CertificatePath, fileName);

            if (File.Exists(storagePath))
                File.Copy(storagePath, locationPath, true);

            if (File.Exists(locationPath))
                return new X509Certificate2(fileName, password);

            return null;
        }
    }
}
