#addin nuget:?package=Cake.Json&version=5.2.0


string target = Argument("target", "Build");
string configuration = Argument("configuration", "Debug");
var json = ParseJsonFromFile("resources/modinfo.json");
string version = (string)json["version"];
string name = (string)json["name"];
string packages = "bin/packages";
string packageFolder = $"{packages}/{name}";
string packageFolderOut = $"{packages}/mods";
string zipFileName = $"{name}_{version}.zip";
string zipfile = $"{packageFolderOut}/{zipFileName}";

string serverData = EnvironmentVariable("VINTAGE_STORY_SERVER_DEV_DATA");



//////////////////////////////////////////////////////////////////////
// TASKS
//////////////////////////////////////////////////////////////////////
Task("CleanPackage")
    .Does(() =>
{
    CleanDirectory(packageFolder);
    DeleteFiles($"{packageFolderOut}/{name}*");
});

Task("Clean")
    .IsDependentOn("CleanPackage")
    .Does(() =>
{
    CleanDirectory("bin/Debug");
    CleanDirectory("bin/Release");
});

Task("Build")
    .IsDependentOn("Clean")
    .Does(() =>
{
    DotNetCoreBuild($"{name}.csproj", new DotNetCoreBuildSettings
    {
        Configuration = configuration
    });
});

Task("Package")
    .IsDependentOn("Build")
    .Does(() =>
{
    EnsureDirectoryExists(packages);
    EnsureDirectoryExists(packageFolder);
    EnsureDirectoryExists(packageFolderOut);
    if (configuration == "Debug")
    {
        CopyFile($"bin/{configuration}/net48/{name}.pdb", $"{packageFolder}/{name}.pdb");
    }
    else
    {
        if (FileExists($"{packageFolder}/{name}.pdb"))
        {
            DeleteFile($"{packageFolder}/{name}.pdb");
        }
    }
    CopyFile($"bin/{configuration}/net48/{name}.dll", $"{packageFolder}/{name}.dll");
    CopyDirectory("resources/", packageFolder);
    Zip(packageFolder, zipfile);
});

Task("Deploy")
    .IsDependentOn("Package")
    .Does(() =>
{
    if (DirectoryExists(serverData))
    {
        CopyFile(zipfile, $"{serverData}/Mods/{zipFileName}");
    }
    else
    {
        throw new Exception($"Server Data directory enviroment variabel is not set: {serverData}");
    }
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
