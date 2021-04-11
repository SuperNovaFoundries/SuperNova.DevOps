using System;
using System.Collections.Generic;
using System.Text;
using System.Threading.Tasks;

namespace SuperNova.DevOps
{
    public interface IBuildProcessor
    {
        Task ProcessBuildAsync(string solutionName, string sourceKey, string sourceBucket, string destinationBucket);
    }



    public class BuildProcessorFactory
    {
        public static IBuildProcessor GetBuildProcessor()
        {
            return new DefaultBuildProcessor();
        }
    }
}
