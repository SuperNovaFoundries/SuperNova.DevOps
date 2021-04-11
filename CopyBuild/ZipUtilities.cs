using System;
using System.IO.Compression;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;
using Amazon;
using Amazon.S3;

namespace SuperNova.DevOps
{
    class ZipUtilities : IDisposable
    {
        private ZipArchive _zipArchive = null;

        public async Task<ZipArchive> GetZipArchiveAsync(string bucket, string key)
        {
            if (_zipArchive == null)
            {
                using var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
                using var zipFileResponse = await s3Client.GetObjectAsync(bucket, key);
                using var zipFileStream = zipFileResponse.ResponseStream;

                _zipArchive = new ZipArchive(zipFileStream);
            }
            return _zipArchive;
        }

        //public async Task<string> GetDestinationKey(string solutionName, string sourceBucket, string s3Key)
        //{
        //    var key = new StringBuilder();
        //    key.Append(solutionName + "/");

        //    var datePart = await GetVersionFromBuildInfoAsync(sourceBucket, s3Key);
        //    key.Append((string.IsNullOrEmpty(datePart) ? DateTime.UtcNow.ToString("yyyyMMddHHmm") : datePart));

        //    if (!string.IsNullOrWhiteSpace(solutionName))
        //    {
        //        key.Append("/" + solutionName + ".zip");
        //    }
        //    return key.ToString();
        //}

        
        internal async Task<string> GetVersionFromBuildInfoAsync(string sourceBucket, string sourceKey)
        {
            var zipArchive = await GetZipArchiveAsync(sourceBucket, sourceKey);
            string dataPart = null;

            var entry = zipArchive.Entries.Where(e => e.FullName.ToUpper().Contains("BUILDINFO")).SingleOrDefault();
            using var stream = entry.Open();

            var doc = XDocument.Load(stream);
            if (doc.Descendants("DLLVERSION").Any())
            {
                var version = doc.Descendants("DLLVERSION").First();
                if(version != null)
                {
                    dataPart = version.Value;
                }
            }
            return dataPart;
        }

        public void Dispose()
        {
            _zipArchive?.Dispose();
        }
    }
}
