using Modpack_Installer.ConfigManager;
using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.Definitions.OnlineDefinitions;
using Modpack_Installer.InstallerAndVerifier;
using Modpack_Installer.ProjectDatabaseManager;
using System.IO.Compression;
using System.Net.Http.Json;
using System.Text.Json;

namespace Modpack_Installer.UserInteractive
{
    //This class provides the functionality the program uses when using the commands
    internal class Functions
    {
        /// <summary>
        /// Gets the Minecraft profile to work with from the user.
        /// </summary>
        /// <param name="minecraftVersions">The list of available Minecraft versions.</param>
        /// <param name="defaultMinecraftPath">The path of default .minecraft directory.</param>
        /// <param name="functionCallerNester">Who called me??</param>
        /// <returns></returns>
        public static (Profile, LogLine) GetDefaultProfile(List<MinecraftVersion> minecraftVersions, string defaultMinecraftPath, string functionCallerNester)
        {
            Profile defaultProfile = new Profile();
            string file = File.ReadAllText(Path.Combine(defaultMinecraftPath, "launcher_profiles.json"));
            if (!File.Exists(file))
                File.Create(file);
            LauncherProfiles profiles = JsonSerializer.Deserialize<LauncherProfiles>(file);
            Console.Clear();
            if (profiles == null) profiles = new LauncherProfiles();
            for (int i = 0; i < profiles.profiles.Count; i++)
            {
                if (profiles.profiles.Values.ElementAt(i).type.Equals("latest-release"))
                    Console.WriteLine($"{i + 1}. Name: Latest release.\n   Version: latest-release.\n   Game directory: {defaultMinecraftPath}\n");
                else if (profiles.profiles.Values.ElementAt(i).type.Equals("latest-snapshot"))
                    Console.WriteLine($"{i + 1}. Name: Latest snapshot.\n   Version: latest-snapshot.\n   Game directory: {defaultMinecraftPath}\n");
                else
                {
                    Console.WriteLine($"{i + 1}. Name: {profiles.profiles.Values.ElementAt(i).name}.\n" +
                        $"   Version: {profiles.profiles.Values.ElementAt(i).lastVersionId}.");
                    if (profiles.profiles.Values.ElementAt(i).gameDir == string.Empty)
                        Console.WriteLine($"   Game directory: {defaultMinecraftPath}\n");
                    else Console.WriteLine($"   Game directory: {profiles.profiles.Values.ElementAt(i).gameDir}\n");
                }

                profiles.profiles.Values.ElementAt(i).id = profiles.profiles.Keys.ElementAt(i);
            }
            Console.WriteLine($"{profiles.profiles.Count + 1}. Paste a custom directory (name and version must be filled manually).\n");

            int result = PromptUserAnswer.IndexAnswers(1, profiles.profiles.Count + 1, $"Enter a valid answer (from 1 to {profiles.profiles.Count + 1})") - 1;
            if (result == profiles.profiles.Count + 1)
            {
                string userAnswer;
                Console.Clear();
                Console.WriteLine("Paste your desired Minecraft installation directory.");
                while (true)
                {
                    userAnswer = Console.ReadLine();
                    if (Directory.Exists(userAnswer)) { defaultProfile.gameDir = userAnswer; break; }
                    else { Console.WriteLine("Enter a valid Minecraft installation directory (where mods and world saves are located)."); }
                }
                Console.Clear();
                Console.WriteLine("Type the version your profile uses (for eg. \"1.16.5\").");
                while (true)
                {
                    userAnswer = Console.ReadLine();
                    if (minecraftVersions.Any(version => userAnswer == version.version)) { defaultProfile.lastVersionId = userAnswer; break; }
                    else { Console.WriteLine("This answer is either invalid or the version doesn't exist."); }
                }
                Console.Clear();
                Console.WriteLine("Type the profile's name (useless but whatever).");
                defaultProfile.name = Console.ReadLine();

                defaultProfile = VersionGrabber.GrabMinecraftVersionFromProfile(defaultProfile, minecraftVersions);

                return (defaultProfile, new LogLine
                {
                    Data = $"{functionCallerNester}GetDefaultProfile: PROFILE_name:{defaultProfile.name}_SET_SUCCESSFULLY",
                    IsSuccessful = true,
                    LogLineState = LogState.Info
                });
            }
            defaultProfile = profiles.profiles.ElementAt(result).Value;
            defaultProfile.gameDir = (profiles.profiles.ElementAt(result).Value.gameDir == string.Empty ? defaultMinecraftPath : profiles.profiles.ElementAt(result).Value.gameDir);
            defaultProfile = VersionGrabber.GrabMinecraftVersionFromProfile(defaultProfile, minecraftVersions);

            return (profiles.profiles.Values.ElementAt(result), new LogLine
            {
                Data = $"{functionCallerNester}GetDefaultProfile: PROFILE_name:{profiles.profiles.Values.ElementAt(result).name}_SET_SUCCESSFULLY",
                IsSuccessful = true,
                LogLineState = LogState.Info
            });
        }

        /// <summary>
        /// Initiates a searching operation in which it prompts the used with results until the desired project is chosen
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectType">The type of the project (mod, modpack or resoucepack)</param>
        /// <param name="defaultProfile">The profile to verify the availability of the projects from the search results</param>
        /// <returns></returns>
        public static async Task<(string, string, string)> Search(HttpClient http, string projectType, Profile defaultProfile)
        {
            int page = 0;
            string userCommand;
            Console.WriteLine($"Type the name of your desired {projectType} to start searching:");
            string query = Console.ReadLine();
            string requestURL;
            SearchData searchResults;
            Console.Clear();

            //Please don't kill me for the next lines of spaghetti...
            while (true)
            {
                requestURL = $"https://api.modrinth.com/v2/search?query={query}&facets=%5B%5B%22project_type%3A{projectType}%22%5D%5D&offset={page * 10}";
                searchResults = await http.GetFromJsonAsync<SearchData>(requestURL);
                int searchResultsCount = searchResults.hits.Count;

                Console.WriteLine($"Showing {projectType}{(searchResultsCount > 1 ? "s" : string.Empty)} for: \u001b[38;2;255;255;255m{query}\n");
                Console.ResetColor();
                for (int i = 0; i < searchResultsCount; i++)
                {

                    if (searchResults.hits[i].color != null)
                    {
                        int[] color = Tools.HexStringToInt(searchResults.hits[i].color.Value.ToString("X"));
                        Console.Write($"\u001b[38;2;{color[0]};{color[1]};{color[2]}m{i + 1}. ");
                        Console.Write($"Name: {searchResults.hits[i].title}\n" +
                            $"   Description: {searchResults.hits[i].description.Replace("\n", "\n     ")}\n   Downloads: {searchResults.hits[i].downloads}" +
                            $"   Follows: {searchResults.hits[i].follows}\u001b[39m");
                        Console.ResetColor();
                    }
                    else
                        Console.Write($"{i + 1}. Name: {searchResults.hits[i].title}\n" +
                            $"   Description: {searchResults.hits[i].description.Replace("\n", "\n     ")}\n   Downloads: {searchResults.hits[i].downloads}" +
                            $"   Follows: {searchResults.hits[i].follows}");
                    if (!ProjectVerifier.IsProjectSupportsProfile(defaultProfile, searchResults.hits[i]))
                        Console.WriteLine("   [\u001b[38;2;255;16;16mNot Compatible!\u001b[39m]\n");
                    else
                        Console.WriteLine("\n");
                }
                Console.WriteLine($"Current page: {page + 1}. Showing {searchResultsCount} result{(searchResultsCount > 1 ? "s" : string.Empty)}. " +
                    $"{searchResults.total_hits} result{(searchResultsCount > 1 ? "s" : string.Empty)} found.\n" +
                    $"Type \"np\" to navigate to the next page{(page > 0 ? ", and \"pp\" to navigate to the previous page." : ".")} Type \"ep\" to exit.\n" +
                    $"Type \"t:\" then the type of mod you want (mod, modpack or resourcepack) to change the mod type.\n" +
                    $"If your desired {projectType} is shown, type the corresponding number to it.");

                userCommand = Console.ReadLine();
                if (int.TryParse(userCommand, out int answer) && answer > 0 && answer <= searchResultsCount)
                {
                    answer--;
                    Console.Clear();
                    if (ProjectVerifier.IsProjectSupportsProfile(defaultProfile, searchResults.hits[answer]))
                    {
                        string message;
                        if (searchResults.hits[answer].color != null)
                        {
                            int[] color = Tools.HexStringToInt(searchResults.hits[answer].color.Value.ToString("X"));
                            message = $"You sure want to install this {projectType}? (yes or no)\n" +
                                $"\u001b[38;2;{color[0]};{color[1]};{color[2]}mName: {searchResults.hits[answer].title}\n" +
                                $"Description:\n{searchResults.hits[answer].description}\n" +
                                $"Downloads: {searchResults.hits[answer].downloads}" +
                                $"   Follows: {searchResults.hits[answer].follows}\u001b[39m";
                            Console.ResetColor();
                        }
                        else
                            message = $"You sure want to install this {projectType}? (yes or no)\n" +
                                $"Name: {searchResults.hits[answer].title}\n" +
                                $"Description:\n{searchResults.hits[answer].description}\nDownloads: {searchResults.hits[answer].downloads}" +
                                $"   Follows: {searchResults.hits[answer].follows}";
                        bool didHeAgree = PromptUserAnswer.YorNAnswer(message);
                        if (didHeAgree) return (searchResults.hits[answer].project_id, searchResults.hits[answer].title, projectType);
                        //                   |____You see here? This is the only state where the function will return, literally
                    }
                    else
                    {
                        Console.WriteLine($"This {projectType} is not supported by your Minecraft profile.\nIt's mostly because you're using an old version or you chose a profile with no mod loader.\nTry using another profile then try again.");
                        Console.ReadLine();
                    }
                }
                else
                {
                    if (userCommand != string.Empty)
                    {
                        if (userCommand.Equals("ep")) return (null, null, null);
                        else if (userCommand.StartsWith("t:"))
                        {
                            if (userCommand.Equals("t:mod")) projectType = "mod";
                            else if (userCommand.Equals("t:modpack")) projectType = "modpack";
                            else if (userCommand.Equals("t:resourcepack")) projectType = "resourcepack";
                        }
                        else if (userCommand.Equals("np")) page++;
                        else if (userCommand.Equals("pp") && page > 0) page--;
                        else { query = userCommand; page = 0; }
                    }
                }

                Console.Clear();
            }
        }

        /// <summary>
        /// To prompt the user for installing a mod, modpack or resourcepack!
        /// </summary>
        /// <param name="http">Required to evade multiple instances of the same HttpClient class.</param>
        /// <returns>The installed project to be added for the installed projects list.</returns>
        public static async Task<(ProjectDatabase, List<VersionData>?, List<LogLine>)> Install(HttpClient http, Profile defaultProfile, ProjectDatabase installedThings, string functionCallerNester)
        {
            List<LogLine> log = new List<LogLine>();
            List<LogLine> currentLogLINES;
            LogLine currentLogLine;
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("modpack.installer.");
            string message = "Choose what you want to install:\n" +
                "mod: to install a mod.\n" +
                "modpack: to install a modpack.\n" +
                "resourcepack: to install a resource pack.\n" +
                "(you have to write the word as shown)\n" +
                "exit: to exit";
            string[] expectedAnswers = { "mod", "modpack", "resourcepack", "exit", "e" };
            int answer = PromptUserAnswer.ValidAnswers(message, expectedAnswers, "Not what I asked!");
            bool didHeAgree;

            if (answer == 3 || answer == 4) return (installedThings, null, log);
            (string projectID, string title, string projectType) = await Search(http, expectedAnswers[answer], defaultProfile);

            if (projectID == null && title == null)
                return (installedThings, null, log);

            Console.Clear();
            Console.WriteLine("Verifying...");

            (VersionData projectData, currentLogLine) =
                await VersionGrabber.GrabSpecificVersionOnline(http, projectID, defaultProfile.mcVersion, (projectType.Equals("resourcepack") ? "minecraft" : defaultProfile.loader), null, $"{functionCallerNester}Install.");
            log.Add(currentLogLine);

            if (projectData == null)
            {
                Console.Clear();
                Console.WriteLine($"This {projectType} is not supported by your Minecraft profile.\nIt's mostly because the {projectType} doesn't have a version that matches your profile's Minecraft version and mod loader.\nTry using another profile then try again.");
                Console.ReadLine();
                return (installedThings, null, null);
            }

            ProjectInstaller installer = new ProjectInstaller(installedThings);
            List<filedata> newInstalledFiles;
            VersionData projectToInstall = projectData;
            //If the latest avilable version is an alpha or beta, we warn the user about that
            if (projectData.version_type != "release")
            {
                message = $"The latest compatible release of {title} is in {projectData.version_type}.\n";
                (VersionData releaseProjectData, currentLogLine) =
                    await VersionGrabber.GrabSpecificVersionOnline(http, projectID, defaultProfile.mcVersion, defaultProfile.loader, "release", $"{functionCallerNester}Install.");
                log.Add(currentLogLine);
                //If there's a stable release avilable within our profile, we notify the user about it
                if (currentLogLine.LogLineState != LogState.Error)
                {
                    expectedAnswers = new string[] { "release", projectData.version_type };
                    message = message + $"But a release version is available.\n" +
                        $"Use the latest {projectData.version_type} version ({projectData.version_number})?" +
                        $" Or the latest stable release ({releaseProjectData.version_number})?\n" +
                        $"release: for using the latest stable release.\n{projectData.version_type}: for using the latest version.\n" +
                        $"(the command should be typed as shown)";
                    answer = PromptUserAnswer.ValidAnswers(message, expectedAnswers, "You have to type the word as shown");
                    if (answer == 0)
                        projectToInstall = releaseProjectData;
                }
                else
                {
                    message = message + "Proceed? (yes or no)";
                    didHeAgree = PromptUserAnswer.YorNAnswer(message);
                    if (!didHeAgree) return (installer.GetInstalledMods(), null, log);
                }
            }

            (newInstalledFiles, List<VersionData> newInstalledProjects, currentLogLINES) =
                await installer.InstallCompleteProject(projectToInstall, defaultProfile, tempDir.FullName, $"{functionCallerNester}Install.", string.Empty);
            log.AddRange(currentLogLINES);

            if (newInstalledFiles == null)
            {
                Console.WriteLine("Nothing is installed.");
                Console.ReadLine();
                return (installer.GetInstalledMods(), null, null);
            }
            Console.WriteLine("Installing complete, now verifying....");
            for (int i = 0; i < newInstalledFiles.Count; i++)
            {
                currentLogLine = ProjectVerifier.IsFileCorrect(newInstalledFiles[i], "Install.");
                log.Add(currentLogLine);
                if (currentLogLine.LogLineState != LogState.Info)
                    Console.WriteLine($"File {newInstalledProjects[i].related_project_title} (of file {newInstalledFiles[i].filename}) is damaged!");
                else
                {
                    Console.WriteLine($"{i + 1}/{newInstalledFiles.Count} is verified");
                    if (Path.GetExtension(newInstalledFiles[i].filename).Equals(".mrpack"))
                    {
                        ZipFile.ExtractToDirectory(newInstalledFiles[i].filename, tempDir.FullName, overwriteFiles: true);
                        if (Directory.Exists(Path.Combine(tempDir.FullName, "overrides\\config")))
                        {
                            Console.Clear();
                            string shouldUpdateConfig;

                            message = $"Let {newInstalledProjects[i].related_project_title} change your mods settings?\n" +
                                $"(changing is recommended for optimization modpacks)";
                            didHeAgree = PromptUserAnswer.YorNAnswer(message);
                            MoveConfigFiles(Path.Combine(tempDir.FullName, "overrides\\config\\"), Path.Combine(defaultProfile.gameDir, "config"), didHeAgree);
                            if (didHeAgree) shouldUpdateConfig = "true";
                            else shouldUpdateConfig = "false";

                            message = $"Should {newInstalledProjects[i].related_project_title} change your mods settings every time it updates?\n" +
                                $"Accepting means this modpack will change your mods settings every time a new update of it comes out.";
                            didHeAgree = PromptUserAnswer.YorNAnswer(message);
                            if (!didHeAgree) shouldUpdateConfig = "ask";

                            for (int j = 0; j < installedThings.database.Count; j++)
                            {
                                if (installedThings.database[j].title == newInstalledProjects[i].related_project_title)
                                    installedThings.database[j].shouldReplaceConfigs = shouldUpdateConfig;
                                break;
                            }
                        }
                        if (File.Exists(Path.Combine(tempDir.FullName, "overrides\\options.txt")))
                        {
                            message = $"Let {newInstalledProjects[i].related_project_title} change your Minecraft settings? (controls, video, audio...)";
                            didHeAgree = PromptUserAnswer.YorNAnswer(message);
                            if (didHeAgree)
                                File.Move(Path.Combine(defaultProfile.gameDir, "options.txt"), Path.Combine(defaultProfile.gameDir, "options.txt"), overwrite: true);
                        }
                    }
                    else if (Path.GetExtension(newInstalledFiles[i].filename).Equals(".jar"))
                        File.Move(newInstalledFiles[i].filename, Path.Combine(defaultProfile.gameDir, "mods\\", Path.GetFileName(newInstalledFiles[i].filename)), overwrite: true);
                    else File.Move(newInstalledFiles[i].filename, Path.Combine(defaultProfile.gameDir, "resourcepacks\\", Path.GetFileName(newInstalledFiles[i].filename)), overwrite: true);
                }
            }

            tempDir.Delete(recursive: true);
            for (int i = 0; i < installedThings.database.Count; i++)
                if (Path.GetExtension(installedThings.database[i].filePath).Equals(".jar") && !installedThings.database[i].filePath.Contains("mods"))
                    installedThings.database[i].filePath = Path.Combine(defaultProfile.gameDir, "mods", installedThings.database[i].filePath);
                else if (Path.GetExtension(installedThings.database[i].filePath).Equals(".zip") && !installedThings.database[i].filePath.Contains("resourcepacks"))
                    installedThings.database[i].filePath = Path.Combine(defaultProfile.gameDir, "resourcpacks", installedThings.database[i].filePath);
            Console.Clear();
            Console.WriteLine($"{newInstalledProjects[0].related_project_title} is now installed!\nEnjoy :)");
            Console.ReadLine();
            return (installer.GetInstalledMods(), newInstalledProjects, log);
        }

        /// <summary>
        /// Moves config files and folders from a downloaded modpack to the game config folder.
        /// </summary>
        /// <param name="downloadedConfigFolder">The config folder path of the downloaded modpack.</param>
        /// <param name="gameConfigFolder">The game config folder path.</param>
        /// <param name="shouldReplace">Should the config files and folders of the downloaded modpack replace existing ones in game config?</param>
        public static void MoveConfigFiles(string downloadedConfigFolder, string gameConfigFolder, bool shouldReplace)
        {
            string[] configFolders = Directory.GetDirectories(downloadedConfigFolder);
            string[] configFiles = Directory.GetFiles(downloadedConfigFolder);

            foreach (string configFile in configFiles)
            {
                string destFileName = Path.Combine(gameConfigFolder, Path.GetFileName(configFile));
                if (File.Exists(destFileName))
                {
                    if (shouldReplace)
                        File.Move(configFile, destFileName, overwrite: true);
                }
                else File.Move(configFile, destFileName);
            }

            foreach (string configFolder in configFolders)
            {
                string destFolderName = Path.Combine(gameConfigFolder, Path.GetFileName(configFolder));
                if (Directory.Exists(destFolderName))
                {
                    if (shouldReplace)
                    {
                        Directory.Delete(destFolderName, recursive: true);
                        Directory.Move(configFolder, destFolderName);
                    }
                }
                else Directory.Move(configFolder, destFolderName);
            }
        }
    }
}
