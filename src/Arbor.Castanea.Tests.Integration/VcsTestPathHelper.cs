using System;
using System.IO;
using System.Linq;
using System.Reflection;
using Arbor.Aesculus.Core;

namespace Arbor.Castanea.Tests.Integration
{
    internal class VcsTestPathHelper
    {
        internal static string FindVcsRootPath()
        {
            try
            {
                Assembly ncrunchAssembly = AppDomain.CurrentDomain.Load("NCrunch.Framework");

                Type ncrunchType =
                    ncrunchAssembly.GetTypes()
                        .FirstOrDefault(
                            type => type.Name.Equals("NCrunchEnvironment", StringComparison.InvariantCultureIgnoreCase));

                MethodInfo method = ncrunchType?.GetMethod("GetOriginalSolutionPath");

                string originalSolutionPath = method?.Invoke(null, null) as string;
                if (!string.IsNullOrWhiteSpace(originalSolutionPath))
                {
                    DirectoryInfo parent = new DirectoryInfo(originalSolutionPath).Parent;
                    return VcsPathHelper.FindVcsRootPath(parent.FullName);
                }
            }
            catch (Exception)
            {
                // ignored
            }
            return VcsPathHelper.FindVcsRootPath();
        }
    }
}