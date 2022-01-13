#addin nuget:?package=Newtonsoft.Json&version=13.0.1
#addin nuget:?package=Cake.Json&version=6.0.1

string target = Argument("target", "Build");
string configuration = Argument("configuration", "Debug");
var json = ParseJsonFromFile("resources/modinfo.json");
string version = (string)json["version"];
string name = (string)json["modid"];
string packages = "bin/packages";
string packageFolder = $"{packages}/{name}";
string packageFolderOut = $"{packages}/mods";
string zipFileName = $"{name}_{version}.zip";
string zipfile = $"{packageFolderOut}/{zipFileName}";

string serverData = EnvironmentVariable("VINTAGE_STORY_SERVER_DATA_MODDING");

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
  if (configuration == "Release")
  {
    if (FileExists($"{packageFolder}/{name}.pdb"))
    {
      DeleteFile($"{packageFolder}/{name}.pdb");
    }
  }
  CopyFiles($"bin/{configuration}/*", $"{packageFolder}/");
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

Task("DeployRemmina")
    .IsDependentOn("Package")
    .Does(() =>
{
  if (DirectoryExists("/home/dilli/remmina-share"))
  {
    CopyFile(zipfile, $"/home/dilli/remmina-share/{zipFileName}");
    CopyFile(zipfile, $"/home/dilli/vmshare/{zipFileName}");
  }
  else
  {
    throw new Exception("Remmina share directory does not exist: /home/dilli/remmina-share");
  }
});

//////////////////////////////////////////////////////////////////////
// EXECUTION
//////////////////////////////////////////////////////////////////////

RunTarget(target);
