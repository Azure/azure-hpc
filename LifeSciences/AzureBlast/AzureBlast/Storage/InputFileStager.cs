// Copyright (c) Microsoft Corporation. All rights reserved.
// Licensed under the MIT License. See License.txt in the project root for license information.

using System;
using System.Collections.Generic;
using System.IO;
using System.Reflection;

namespace Microsoft.Azure.Batch.Blast.Storage
{
    public class InputFileStager
    {
        private const string ScriptContainer = "blast-scritps";

        public static List<ResourceFile> StageImportScripts(
            IBlobStorageProvider blobStorageProvider,
            string containerName = ScriptContainer)
        {
            var resourceFiles = new List<ResourceFile>();
            var path = GetImportScriptsPath();
            foreach (var filepath in Directory.EnumerateFiles(path, "*.*", SearchOption.TopDirectoryOnly))
            {
                var filename = Path.GetFileName(filepath);
                var content = File.ReadAllText(filepath).Replace("\r\n", "\n"); // Remove Windows line endings
                blobStorageProvider.UploadBlobFromText(containerName, filename, content);
                resourceFiles.Add(
                    new ResourceFile(
                        blobStorageProvider.GetBlobSAS(containerName, filename),
                        filename));
            }
            return resourceFiles;
        }

        private static string GetImportScriptsPath()
        {
            string codeBase = Assembly.GetExecutingAssembly().CodeBase;
            UriBuilder uri = new UriBuilder(codeBase);
            string path = Uri.UnescapeDataString(uri.Path);
            var assemblyPath = Path.GetDirectoryName(path);
            return Path.Combine(assemblyPath, "BatchScripts");
        }
    }
}
