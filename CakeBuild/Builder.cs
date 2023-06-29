using System.Linq;
using Cake.Common.IO;
using Cake.Common.Tools.DotNet;
using Cake.Common.Tools.DotNet.Build;
using Cake.Core;
using Cake.Frosting;
using Cake.Json;
using Vintagestory.API.Common;

namespace Build;

public static class Builder
{
    public static int Main(string[] args)
    {
        return new CakeHost()
            .UseContext<BuildContext>()
            .Run(args);
    }
}

public class BuildContext : FrostingContext
{
    
    public string Version {get; }
    public string Name {get; }
    public string Packages {get; }
    public string PackageFolder {get; }
    public string PackageFolderOut {get; }
    public string ZipFileName {get; }
    public string ZipFile {get; }

    public BuildContext(ICakeContext context)
        : base(context)
    {
        var modInfo = context.DeserializeJsonFromFile<ModInfo>("../resources/modinfo.json");
        Version = modInfo.Version;
        Name = modInfo.ModID;
        Packages = $"../{Name}/bin/packages";
        PackageFolder = $"{Packages}/{Name}";
        PackageFolderOut = $"{Packages}/mods";
        ZipFileName = $"{Name}_{Version}.zip";
        ZipFile = $"{PackageFolderOut}/{ZipFileName}";
    }
}


[TaskName("Build")]
public sealed class BuildTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.DotNetBuild($"../{context.Name}/{context.Name}.csproj",new DotNetBuildSettings{Configuration = "Release"});
    }
}

[TaskName("Package")]
[IsDependentOn(typeof(BuildTask))]
public sealed class PackageTask : FrostingTask<BuildContext>
{
    public override void Run(BuildContext context)
    {
        context.EnsureDirectoryExists(context.Packages);
        context.EnsureDirectoryExists(context.PackageFolder);
        context.EnsureDirectoryExists(context.PackageFolderOut);
        context.CleanDirectory(context.PackageFolderOut);
        var filePathCollection = context.GetFiles($"../{context.Name}/bin/Release/*").Where(f => !f.FullPath.Contains("Newtonsoft"));
        context.CopyFiles(filePathCollection, $"{context.PackageFolder}/");
        context.CopyDirectory("../resources/", context.PackageFolder);
        context.Zip(context.PackageFolder, context.ZipFile);
    }
}


[TaskName("Default")]
[IsDependentOn(typeof(PackageTask))]
public class DefaultTask : FrostingTask
{
}