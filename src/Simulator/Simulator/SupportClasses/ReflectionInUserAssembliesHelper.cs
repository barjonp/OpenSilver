

/*===================================================================================
* 
*   Copyright (c) Userware (OpenSilver.net, CSHTML5.com)
*      
*   This file is part of both the OpenSilver Simulator (https://opensilver.net), which
*   is licensed under the MIT license (https://opensource.org/licenses/MIT), and the
*   CSHTML5 Simulator (http://cshtml5.com), which is dual-licensed (MIT + commercial).
*   
*   As stated in the MIT license, "the above copyright notice and this permission
*   notice shall be included in all copies or substantial portions of the Software."
*  
\*====================================================================================*/



using System;
using System.IO;
using System.Reflection;
using System.Windows;
using System.Linq;
using System.Collections.Generic;
using DotNetForHtml5.Compiler;

namespace DotNetForHtml5.EmulatorWithoutJavascript
{
    static class ReflectionInUserAssembliesHelper
    {
        private static Assembly _coreAssembly;

        public static Type GetTypeFromCoreAssembly(string typeFullName)
        {
            EnsureCoreAssemblyIsLoaded();

            return _coreAssembly.GetType(typeFullName);
        }

        private static void EnsureCoreAssemblyIsLoaded()
        {
            if (_coreAssembly == null)
            {
                _coreAssembly = GetCoreAssembly();
                if (_coreAssembly == null)
                {
                    MessageBox.Show("Could not find the core assembly among the loaded assemblies.");
                }
            }
        }

        private static Assembly GetCoreAssembly()
        {
            return (from a in AppDomain.CurrentDomain.GetAssemblies()
                    where (string.Equals(a.GetName().Name, Constants.NAME_OF_CORE_ASSEMBLY, StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(a.GetName().Name, Constants.NAME_OF_CORE_ASSEMBLY_USING_BRIDGE, StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(a.GetName().Name, Constants.NAME_OF_CORE_ASSEMBLY_SLMIGRATION, StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(a.GetName().Name, Constants.NAME_OF_CORE_ASSEMBLY_SLMIGRATION_USING_BRIDGE, StringComparison.CurrentCultureIgnoreCase)
                    || string.Equals(a.GetName().Name, Constants.NAME_OF_CORE_ASSEMBLY_USING_BLAZOR, StringComparison.CurrentCultureIgnoreCase))
                    select a).FirstOrDefault();
        }

        internal static bool TryGetPathOfAssemblyThatContainsEntryPoint(out string path)
        {
            path = null;
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length >= 2 && !string.IsNullOrEmpty(commandLineArgs[1]))
            {
                path = commandLineArgs[1];

                // If path is not absolute, make it absolute:
                string currentDirectory = null;
                bool isPathRelative =
                    !Path.IsPathRooted(path)
                    && !string.IsNullOrEmpty(path)
                    && !string.IsNullOrEmpty(currentDirectory = Directory.GetCurrentDirectory());
                if (isPathRelative)
                {
                    path = Path.Combine(currentDirectory, path);
                }

                return true;
            }
            else
                return false;
        }

        internal static bool TryGetCustomBaseUrl(out string customBaseUrl)
        {
            const string prefix = "/baseurl:";
            customBaseUrl = null;
            string[] commandLineArgs = Environment.GetCommandLineArgs();
            if (commandLineArgs.Length >= 3
                && !string.IsNullOrEmpty(commandLineArgs[2])
                && commandLineArgs[2].StartsWith(prefix))
            {
                // Remove the prefix:
                customBaseUrl = commandLineArgs[2].Substring(prefix.Length);

                // Remove the quotes if any:
                customBaseUrl = customBaseUrl.Trim('"');

                return true;
            }
            else
                return false;
        }

        internal static void GetOutputPathsByReadingAssemblyAttributes(
            Assembly entryPointAssembly,
            out string outputRootPath,
            out string outputAppFilesPath,
            out string outputLibrariesPath,
            out string outputResourcesPath,
            out string intermediateOutputAbsolutePath)
        {
            // todo: see if the path can be not hard-coded
            // In the OpenSilver version, the app use the wwwroot folder to store the libs and resources
            // This folder is not inside the build dir (bin\Debug\netstandard2.0) but at the root level
            outputRootPath = @"..\..\..\wwwroot\"; 
            outputLibrariesPath = @"\app-cshtml5\libs\";
            outputResourcesPath = @"\app-cshtml5\res\";
            outputAppFilesPath = @"";
            intermediateOutputAbsolutePath = @"";
        }
    }
}
