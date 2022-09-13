namespace Microsoft.eShopOnContainers.Services.Identity.API.Certificates
{
    /// <summary>
    /// 设置签名凭据
    /// </summary>
    static class Certificate
    {
        public static X509Certificate2 Get()
        {
            var assembly = typeof(Certificate).GetTypeInfo().Assembly;
            var names = assembly.GetManifestResourceNames();

            /***********************************************************************************************
             *  请注意，这里我们仅将本地证书用于测试目的。 在真实环境中，证书应该以安全的方式创建和存储，这超出了本项目的范围
             **********************************************************************************************/
            using var stream = assembly.GetManifestResourceStream("Identity.API.Certificate.idsrv3test.pfx");
            return new X509Certificate2(ReadStream(stream), "idsrv3test");
        }

        private static byte[] ReadStream(Stream input)
        {
            byte[] buffer = new byte[16 * 1024];
            using MemoryStream ms = new MemoryStream();
            int read;
            while ((read = input.Read(buffer, 0, buffer.Length)) > 0)
            {
                ms.Write(buffer, 0, read);
            }
            return ms.ToArray();
        }
    }
}