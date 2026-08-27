using Modpack_Installer.ConfigManager;
using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.Definitions.OnlineDefinitions;
using Modpack_Installer.InstallerAndVerifier;
using Modpack_Installer.ProjectDatabaseManager;
using Modpack_Installer.UserInteractive;
using System.Diagnostics;
using System.Net.Http.Json;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using System.Text;

/*
 * Welp, here we are, at 26th of August, 2026.
 * And after a full month (since 26th of July) I've finally finished this.
 * Even though it took me way more than I anticipated.
 * So for anyone reading this... GET READY FOR THE RIDE!!
 * You'll see the worst code in your entire life! Thank me later.
 * 
 * note: please don't ask me why I used LogLine everywhere...
 */

namespace Modpack_Installer
{
    //This class is made by AI. I'm sorry but I'm just too lazy to do it myself
    public static class ConsoleHelper
    {
        private const int STD_OUTPUT_HANDLE = -11;
        private const uint ENABLE_VIRTUAL_TERMINAL_PROCESSING = 0x0004;

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern IntPtr GetStdHandle(int nStdHandle);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool GetConsoleMode(IntPtr hConsoleHandle, out uint lpMode);

        [DllImport("kernel32.dll", SetLastError = true)]
        private static extern bool SetConsoleMode(IntPtr hConsoleHandle, uint dwMode);

        public static void EnableVirtualTerminal()
        {
            // Set console output encoding to UTF-8
            Console.OutputEncoding = Encoding.UTF8;

            // VT processing is a Windows-specific setting; Linux/macOS enable ANSI by default
            if (OperatingSystem.IsWindows())
            {
                IntPtr handle = GetStdHandle(STD_OUTPUT_HANDLE);
                if (handle != IntPtr.Zero && GetConsoleMode(handle, out uint mode))
                {
                    mode |= ENABLE_VIRTUAL_TERMINAL_PROCESSING;
                    SetConsoleMode(handle, mode);
                }
            }
        }
    }

    internal class Program
    {
        //Paths
        static string programConfigFolder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "modpack-installer\\");
        static string programConfigPath = Path.Combine(programConfigFolder, "modpack-installer.config");
        static string installedProjectsSavedFilePath = string.Empty;
        static string defaultMinecraftDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), ".minecraft");
        static string modsPath = string.Empty;
        static string resourcpacksPath = string.Empty;
        static string configPath = string.Empty;

        //Objects
        static ConfigEditor configFile = new ConfigEditor(programConfigPath);
        static HttpClient http = new HttpClient();
        static List<MinecraftVersion> minecraftVersions;
        static ProjectDatabase installedProjects;
        static Profile workingProfile;

        //Settings vars
        static bool shouldAutoUpdate = false;
        static bool shouldUpdateSelectedProfile = false;

        //For logs and logging and stuff
        static List<LogLine> log = new List<LogLine>();
        static List<LogLine> tempLogList = new List<LogLine>();
        static LogLine tempLog = new LogLine();

        /// <summary>
        /// If the program runs for the first time, this function prompts the user for selecting the profile and setting some options
        /// </summary>
        /// <returns>Probably, a task (which I don't know what that means)</returns>
        public static async Task FirstTime()
        {
            if (!Directory.Exists(programConfigFolder))
                Directory.CreateDirectory(programConfigFolder);

            Console.Clear();
            Console.WriteLine("Welcome!\nLooks like it's the first time you run this program, we will guide you through the steps to get it set up.");
            Console.ReadKey();
            Console.Clear();

            if (!Directory.Exists(defaultMinecraftDir))
            {
                Console.WriteLine("Looks like the default Minecraft directory (.minecraft) have not been found in the default path.\n" +
                    "Paste the path of your default Minecraft directory. (the path leading to .minecraft folder)");
                while (true)
                {
                    string userAnswer = Console.ReadLine();
                    if (Directory.Exists(userAnswer))
                    {
                        Console.Clear();
                        defaultMinecraftDir = userAnswer;
                        break;
                    }
                        
                    else Console.WriteLine("Paste a valid directory path.");
                }
            }

            configFile.SetValue("default_game_dir", defaultMinecraftDir);
            Console.WriteLine("First, choose your Minecraft profile from the list below:");
            (workingProfile, tempLog) = Functions.GetDefaultProfile(minecraftVersions, defaultMinecraftDir, "Main.");
            log.Add(tempLog);
            configFile.SetValue("id", workingProfile.id);
            installedProjectsSavedFilePath = Path.Combine(programConfigFolder, $"modpack-installer.installedfiles.{configFile.GetValue("id")}.json");
            installedProjects = new ProjectDatabase(installedProjectsSavedFilePath);

            bool didHeAgree = PromptUserAnswer.YorNAnswer("Set this profile as default? (yes or no)\nThe program will automatically use the profile in the " +
                "next launch");
            if (didHeAgree)
            {
                configFile.SetValue("default", "true");
                configFile.SetValue("profile_name", workingProfile.name);
                configFile.SetValue("game_directory", workingProfile.gameDir);
                configFile.SetValue("version", workingProfile.mcVersion);
                configFile.SetValue("loader", workingProfile.loader);
            }
            else configFile.SetValue("default", "false");

            modsPath = Path.Combine(defaultMinecraftDir, "mods");
            resourcpacksPath = Path.Combine(defaultMinecraftDir, "resourcepacks");
            configPath = Path.Combine(defaultMinecraftDir, "config");

            Console.WriteLine("All mods and resourcepacks will be deleted in order to use the program.\nPress enter to continue...");
            Console.ReadLine();
            if (Directory.Exists(modsPath)) Directory.Delete(modsPath, recursive: true);
            if (Directory.Exists(resourcpacksPath)) Directory.Delete(resourcpacksPath, recursive: true);
            if (Directory.Exists(configPath)) Directory.Delete(configPath, recursive: true);
            Directory.CreateDirectory(modsPath);
            Directory.CreateDirectory(resourcpacksPath);
            Directory.CreateDirectory(configPath);

            Console.Clear();
            Console.WriteLine("All done!\nNow it's time to add your favourite mods!");
            Console.ReadLine();

            while (true)
            {
                (installedProjects, List<VersionData> tempNewInstalledProjects, tempLogList) =
                    await Functions.Install(http, workingProfile, installedProjects, "Main.FirstTime.");
                if (tempNewInstalledProjects == null)
                {
                    if (tempLogList != null)
                        break;
                }
                else
                {
                    didHeAgree = PromptUserAnswer.YorNAnswer("Install another? (yes or no)");
                    log.AddRange(tempLogList);
                    if (!didHeAgree)
                        break;
                }

            }

            Console.Clear();
            didHeAgree =
                PromptUserAnswer.YorNAnswer("Do you want the program to check for updates automatically upon launch? (yes or no)");
            if (didHeAgree)
                configFile.SetValue("auto_update", "true");
            else configFile.SetValue("auto_update", "false");

            configFile.SetValue("ran_before", "true");
            Console.WriteLine("All finished!");
            Console.ReadLine();
        }

        static async Task Main(string[] args)
        {
            Console.Title = "Modpack Installer";

            ConsoleHelper.EnableVirtualTerminal();

            try { minecraftVersions = await http.GetFromJsonAsync<List<MinecraftVersion>>("https://api.modrinth.com/v2/tag/game_version"); }
            catch (Exception e)
            {
                Console.WriteLine($"There's no internet connection.\nConnect to the internet then try again.\nError: {e.Message}");
                Console.ReadLine();
            }
            Console.Clear();
            if (File.Exists(programConfigPath) && configFile.GetValue("ran_before") != null && configFile.GetValue("ran_before").Equals("true"))
                LoadSettings();
            else
            {
                await FirstTime();
                installedProjects.Save();
            }

            if (shouldUpdateSelectedProfile)
            {
                try
                {
                    (workingProfile, tempLog) = Functions.GetDefaultProfile(minecraftVersions, defaultMinecraftDir, "Main.");
                    log.Add(tempLog);
                }
                catch (Exception e) { ReportCrash(e); }
            }

            for (int i = 0; i < installedProjects.database.Count; i++)
            {
                ProjectVersion currentProject = installedProjects.database[i];
                if (!Path.GetExtension(currentProject.filePath).Equals(".mrpack") && !File.Exists(currentProject.filePath))
                {
                    Console.Clear();
                    string[] expectedAnswers = { "redownload", "download", "remove", "r" };
                    string message = $"The file of {currentProject.title} is not found, it is mostly deleted.\nDo you want to redownload it?" +
                        $" Or remove it completely from the program's database?\n(type remove or download)";
                    int answer = PromptUserAnswer.ValidAnswers(message, expectedAnswers, "You have to type either remove or download");
                    if (answer < 2)
                    {
                        Console.WriteLine("Downloading...");
                        (VersionData projectVersionData, tempLog) =
                            await ProjectRequester.GetProjectSpecificVersion(http, currentProject.projectID, currentProject.versionID, "Main.");
                        log.Add(tempLog);
                        (_, tempLogList) =
                            await ProjectInstaller.InstallProjectFiles(projectVersionData, http, Path.GetFullPath(currentProject.filePath), onlyFirstFile: true, "Main.");
                        log.AddRange(tempLog);
                        Console.WriteLine("Download complete!");
                        Console.ReadLine();
                    }
                    else
                    {
                        installedProjects.RemoveProject(currentProject.projectID);
                        Console.WriteLine($"{currentProject.title} removed successfully.");
                        Console.ReadLine();
                    }
                }
                installedProjects.Save();
            }
            Console.Clear();

            if (shouldAutoUpdate)
            {
                try
                {
                    (installedProjects, tempLogList) = await ProjectUpdater.UpdateAllProjects(installedProjects, workingProfile, http, "Main.AutoUpdate.");
                    log.AddRange(tempLogList);
                }
                catch (Exception e) { ReportCrash(e); }
            }

            Console.Clear();

            while (true)
            {
                string message = $"Currently using {(workingProfile.id == string.Empty ? "custom profile: " : "profile: ")}{workingProfile.name}\n" +
                    $"What you wanna do now?\n" +
                $"install: install a new mod (mod, modpack, resourcepack)\n" +
                $"update: updates all or specefic mods\n" +
                $"remove: remove a mod\n" +
                $"options: manage the tool's options and settings\n" +
                $"exit: to close the program, of course\n" +
                $"(type either the whole word or the first letter of it)";
                string[] expectedAnswers = { "install", "i", "update", "u", "remove", "r", "options", "o", "exit", "e" };
                int answer =
                    PromptUserAnswer.ValidAnswers(message, expectedAnswers, "You have to type either the full word (install), or the first letter of it (i)");

                if (answer == 0 || answer == 1)
                {
                    (installedProjects, List<VersionData> newInstalledProjects, tempLogList) = await Functions.Install(http, workingProfile, installedProjects, "Main.");
                    if (tempLogList != null)
                        log.AddRange(tempLogList);
                    installedProjects.Save();
                }
                else if (answer == 2 || answer == 3)
                {
                    (installedProjects, _) = await ProjectUpdater.UpdateAllProjects(installedProjects, workingProfile, http, "Main.");
                }
                else if (answer == 4 || answer == 5)
                {
                    RemoveProject();
                }
                else if (answer == 6 || answer == 7)
                {
                    EditSettings();
                }
                else if (answer == 8 || answer == 9)
                {
                    StringBuilder logFileContents = new StringBuilder();
                    foreach (LogLine logLine in log)
                        logFileContents.AppendLine($"{logLine.IsSuccessful} {logLine.LogLineState} {logLine.Data}");
                    File.WriteAllText(Path.Combine(programConfigFolder, "latest-log.txt"), logFileContents.ToString());
                    installedProjects.Save();
                    Environment.Exit(0);
                }
            }

            Console.ReadKey();
        }

        /// <summary>
        /// Loads settings from config file, duh
        /// </summary>
        static void LoadSettings()
        {
            if (configFile.GetValue("default").Equals("true"))
            {
                workingProfile = new Profile
                {
                    id = configFile.GetValue("id"),
                    name = configFile.GetValue("profile_name"),
                    gameDir = configFile.GetValue("game_directory"),
                    mcVersion = configFile.GetValue("version"),
                    loader = configFile.GetValue("loader"),
                };
                defaultMinecraftDir = configFile.GetValue("default_game_dir");
            }
            else shouldUpdateSelectedProfile = true;
            if (configFile.GetValue("auto_update").Equals("true"))
                shouldAutoUpdate = true;
            installedProjectsSavedFilePath = Path.Combine(programConfigFolder, $"modpack-installer.installedfiles.{configFile.GetValue("id")}.json");
            installedProjects = new ProjectDatabase(installedProjectsSavedFilePath);


        }

        /// <summary>
        /// Prompts the user for editing the program's settings
        /// </summary>
        static void EditSettings()
        {
            string[] expectedAnswers = { "exit", "e", "profile", "p", "defpro", "d", "autou", "a" };
            while (true)
            {
                string message = "This is the settings page. Here you can change the program's settings\n" +
                    $"profile: change the current working Minecraft profile (changes Minecraft version and mod loader compatibility).\n" +
                    $"defpro: whether the program should use the currently using Minecraft profile automatically upon launch or not. {(shouldUpdateSelectedProfile ? "Disabled" : "Enabled")}.\n" +
                    $"autou: whether the program should auto update the mods upon launch or not. {(shouldAutoUpdate ? "Enabled" : "Disabled")}.\n" +
                    $"exit: go back to homepage.\n" +
                    "(type either the whole word or the first letter of it)";
                int answer = PromptUserAnswer.ValidAnswers(message, expectedAnswers, "Choose with the word that corresponds to the option");
                bool didHeAgree = false;
                Console.Clear();
                if (answer == 0 || answer == 1) return;
                else if (answer == 2 || answer == 3)
                {
                    (workingProfile, tempLog) = Functions.GetDefaultProfile(minecraftVersions, defaultMinecraftDir, "Main.EditSettings.");
                    log.Add(tempLog);
                    installedProjects.Save();
                    configFile.SetValue("id", workingProfile.id);
                    configFile.SetValue("profile_name", workingProfile.name);
                    configFile.SetValue("game_directory", workingProfile.gameDir);
                    configFile.SetValue("version", workingProfile.mcVersion);
                    configFile.SetValue("loader", workingProfile.loader);
                    Console.WriteLine("The program should restart in order to apply changes.");
                    Console.ReadLine();
                    Process.Start(Environment.ProcessPath);
                    Environment.Exit(0);
                }
                else if (answer == 4 || answer == 5)
                {
                    message = $"Should the program automatically choose the currently using Minecraft profile when launch?\n" +
                        $"The options is currently {(shouldUpdateSelectedProfile ? "disabled" : "enabled")}.";
                    didHeAgree = PromptUserAnswer.YorNAnswer(message);
                    if (didHeAgree)
                    { configFile.SetValue("default", "true"); shouldUpdateSelectedProfile = false; }
                    else
                    { configFile.SetValue("default", "false"); shouldUpdateSelectedProfile = true; }
                }
                else if (answer == 6 || answer == 7)
                {
                    message = $"Should the program automatically update installed mods when launch?\n" +
                        $"The options is currently {(shouldAutoUpdate ? "enabled" : "disabled")}.";
                    didHeAgree = PromptUserAnswer.YorNAnswer(message);
                    if (didHeAgree)
                    { configFile.SetValue("auto_update", "true"); shouldAutoUpdate = true; }
                    else
                    { configFile.SetValue("auto_update", "false"); shouldAutoUpdate = false; }
                }
            }
        }

        /// <summary>
        /// Prompts the user for removing any installed project
        /// </summary>
        public static void RemoveProject()
        {
            int longestFileNameLength = 0;
            StringBuilder message = new StringBuilder();

            Console.Clear();

            if (installedProjects.database.Count == 0)
            {
                Console.WriteLine("There are no installed mods to remove.\nWhat? Do you want to remove void?\n");
                Console.ReadLine();
                return;
            }
            while (true)
            {
                message.Clear();

                for (int i = 0; i < installedProjects.database.Count; i++)
                    if (longestFileNameLength < installedProjects.database[i].title.Length)
                        longestFileNameLength = installedProjects.database[i].title.Length + 1;

                message.AppendLine($"No.  ".Substring(0, 4) + $"Mod name:                                      ".Substring(0, longestFileNameLength) +
                        $"  Mod file name:");

                for (int i = 0; i < installedProjects.database.Count; i++)
                    message.AppendLine($"{i + 1}.  ".Substring(0, 4) + $"{installedProjects.database[i].title}.                                      ".Substring(0, longestFileNameLength) +
                        $"  {Path.GetFileName(installedProjects.database[i].filePath)}");

                Console.WriteLine(message.ToString() + "Type the corresponding number to the desired mod. Type \"exit\" or \'e\' to exit.");
                string userAnswer = Console.ReadLine();
                if (int.TryParse(userAnswer, out int index))
                {
                    if (index > 0 && index <= installedProjects.database.Count)
                    {
                        Console.Clear();
                        ProjectVersion selectedProject = installedProjects.GetProject(index - 1);
                        string promptMessage = $"You sure want to remove this?\n" +
                            $"\u001b[38;2;255;255;255mName:\u001b[39m\n  {selectedProject.title}\n" +
                            $"\u001b[38;2;255;255;255mDescription:\u001b[39m\n  {selectedProject.description.Replace("\n", "\n  ")}\n" +
                            $"\u001b[38;2;255;255;255mMod file path:\u001b[39m\n  {selectedProject.filePath}";

                        bool didHeAgree = PromptUserAnswer.YorNAnswer(promptMessage);
                        if (didHeAgree)
                        {
                            File.Delete(selectedProject.filePath);
                            installedProjects.RemoveProject(selectedProject.projectID);
                            installedProjects.Save();
                            Console.WriteLine("Removed successfully.");
                            Console.ReadLine();
                            Console.Clear();
                        }
                        else Console.Clear();
                    }
                    else
                    {
                        Console.Clear();
                        Console.WriteLine($"Type a number from 1 to {installedProjects.database.Count}");
                    }
                }
                else if (userAnswer.ToLower().Equals("exit") || userAnswer.ToLower().Equals("e"))
                    break;
                else
                {
                    Console.Clear();
                    Console.WriteLine($"Numbers are only accepted");
                }
            }
        }

        /// <summary>
        /// Reports the crash to the user and saves the log file
        /// </summary>
        public static void ReportCrash(Exception e)
        {
            log.Add(new LogLine
            {
                IsSuccessful = false,
                LogLineState = LogState.Error,
                Data = $"The program crashed:\nError message: \"{e.Message}\". Error stack: \"{e.StackTrace}\". Error causer: \"{e.Source}\"" +
                    $"\nI hope I can fix the problem..."
            });
            StringBuilder logFileContents = new StringBuilder();
            foreach (LogLine logLine in log)
                logFileContents.AppendLine($"{logLine.IsSuccessful} {logLine.LogLineState} {logLine.Data}");
            File.WriteAllText(Path.Combine(programConfigFolder, "latest-log.txt"), logFileContents.ToString());
            Console.WriteLine("CRASH!!\nSomething went wrong...\nIs it because of your internet?\nI think it's mostly because how I handle data...\n" +
                "Welp whatever, the log is at: " + Path.Combine(programConfigFolder, "latest-log.txt"));
            Console.ReadLine();
        }
    }
}
