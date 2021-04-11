using Amazon.Lambda.Core;
using Amazon.Lambda.S3Events;
using Common;
using Microsoft.Extensions.Logging;
using System;
using System.Composition;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SuperNova.DevOps
{

    class CopyBuildLambda
    {
        [Import]
        private IServiceLoggerFactory _logFactory { get; set; } = null;
        private ILogger _logger;
        public CopyBuildLambda()
        {
            MEFLoader.SatisfyImportsOnce(this);
            _logger = _logFactory.GetLogger("SuperNova.DevOps::CopyBuild");
        }

        [LambdaSerializer(typeof(Amazon.Lambda.Serialization.Json.JsonSerializer))]
        public async Task RunAsync(S3Event s3Event, ILambdaContext context)
        {
            _logger.LogInformation("Copy Build Triggered: ");
            _logger.LogInformation(s3Event.ToJsonString(true));

            //used by third party zip library
            Encoding.RegisterProvider(CodePagesEncodingProvider.Instance);

            if (s3Event == null || s3Event.Records == null || s3Event.Records.Count <= 0) return;

            foreach (var record in s3Event.Records.Where(r => r.S3.Object.Key.ToUpper().Contains("BUILD")))
            {
                var buildBucket = Environment.GetEnvironmentVariable("BUILDBUCKET");
                _logger.LogInformation("Build bucket: " + buildBucket);

                var sourceKey = record.S3.Object.Key;
                _logger.LogInformation($"Source Key: {sourceKey}");

                var solutionName = sourceKey.Substring(0, sourceKey.IndexOf("/"));
                _logger.LogInformation($"Solution: {solutionName}");


                await BuildProcessorFactory.GetBuildProcessor().ProcessBuildAsync(solutionName, sourceKey, record.S3.Bucket.Name, buildBucket);
            }
        }

    }
}
