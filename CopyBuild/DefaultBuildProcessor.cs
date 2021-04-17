using Amazon;
using Amazon.S3;
using Amazon.S3.Model;
using System.IO;
using System.IO.Compression;
using System.Linq;
using System.Threading.Tasks;

namespace SuperNova.DevOps
{
    internal class DefaultBuildProcessor : IBuildProcessor
    {
        public async Task ProcessBuildAsync(string solutionName, string sourceKey, string sourceBucket, string destinationBucket)
        {

            var tempPath = Path.Combine(Path.GetTempPath(), solutionName.Replace(".", ""));
            if (!Directory.Exists(tempPath)) Directory.CreateDirectory(tempPath);

            DeleteDirectoryContents(tempPath);
            
            using var zipUtil = new ZipUtilities();
            var archive = await zipUtil.GetZipArchiveAsync(sourceBucket, sourceKey);
            
            foreach (var entry in archive.Entries.Distinct())
            {
                if (entry.Name.Length <= 0) continue;
                if (entry.FullName.ToLower().Contains("/runtimes/"))
                {
                    var runtimesPath = entry.FullName.Split("/runtimes/")[1];
                    var fullPath = Path.Combine(tempPath, "runtimes", runtimesPath);

                    var directoryName = Path.GetDirectoryName(fullPath);
                    if (!Directory.Exists(directoryName)) Directory.CreateDirectory(directoryName);

                    entry.ExtractToFile(fullPath);
                }
                else
                {
                    entry.ExtractToFile(Path.Combine(tempPath, entry.Name));
                }
            }

            var version = await zipUtil.GetVersionFromBuildInfoAsync(sourceBucket, sourceKey);
            foreach (var template in Directory.EnumerateFiles(tempPath, "*.template"))
            {
                InjectVersionIntoTemplate(template, version);
            }

            var zipFileName = $"{solutionName}.{version}.zip";
            var zipFullname = Path.Combine(tempPath, zipFileName);
            using Ionic.Zip.ZipFile zip = new Ionic.Zip.ZipFile();
            zip.AddDirectory(tempPath);
            zip.Save(zipFullname);

            using var s3Client = new AmazonS3Client(RegionEndpoint.USEast1);
            await s3Client.PutObjectAsync(new PutObjectRequest
            {
                BucketName = destinationBucket,
                Key = $"{solutionName}/{version}/{zipFileName}",
                FilePath = zipFullname
            });
           
        }

        private void InjectVersionIntoTemplate(string template, string version)
        {
            var fileText = File.ReadAllText(template).Replace(".zip", $".{version}.zip");
            fileText = fileText.Replace("12j7jz", version);
            File.WriteAllText(template, fileText);
        }

        private void DeleteDirectoryContents(string tempPath)
        {
            var di = new DirectoryInfo(tempPath);
            foreach(var fi in di.EnumerateFiles())
            {
                fi.Delete();
            }
            foreach(var d in di.EnumerateDirectories())
            {
                d.Delete();
            }
        }
    }
}