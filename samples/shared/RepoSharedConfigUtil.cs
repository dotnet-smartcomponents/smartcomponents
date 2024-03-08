// Licensed to the .NET Foundation under one or more agreements.
// The .NET Foundation licenses this file to you under the MIT license.

using System.Reflection;

public static class RepoSharedConfigUtil
{
    public static void AddRepoSharedConfig(this IConfigurationBuilder configuration)
    {
        // This is only used within this repo to simplify sharing config
        // across multiple projects. For real usage, just add the required
        // config values to your appsettings.json file.

        var envVarPath = Environment.GetEnvironmentVariable("SMARTCOMPONENTS_REPO_CONFIG_FILE_PATH");
        if (!string.IsNullOrEmpty(envVarPath))
        {
            configuration.AddJsonFile(envVarPath);
            return;
        }

        var dir = Path.GetDirectoryName(Assembly.GetEntryAssembly()!.Location)!;
        while (true)
        {
            var path = Path.Combine(dir, "RepoSharedConfig.json");
            if (File.Exists(path))
            {
                configuration.AddJsonFile(path);
                return;
            }

            var parent = Directory.GetParent(dir);
            if (parent == null)
            {
                throw new FileNotFoundException("Could not find RepoSharedConfig.json");
            }

            dir = parent.FullName;
        }
    }
}
