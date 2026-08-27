using Modpack_Installer.ConfigManager;
using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.ProjectDatabaseManager;
using Modpack_Installer.UserInteractive;
using System.IO.Compression;

namespace Modpack_Installer.InstallerAndVerifier
{
    internal class ProjectInstaller(ProjectDatabase installedMods)
    {
        string indexer = "";
        /// <summary>
        /// Installs the project with its files and dependencies
        /// </summary>
        /// <param name="defaultProfile">The profile to grab the most compatible version of the project and its dependencies</param>
        /// <param name="projectVersion">The version data of the project to be installed</param>
        /// <param name="destDir">The destination directory path for the installed files</param>
        /// <param name="functionCallerNester">Who called me??</param>
        /// <param name="dependencyProgresser">The counter that shows the index of current downloading dependency and the count of dependencies to be downloaded</param>
        /// <returns>A list of installed files. A list of the version data of each dependency installed. A list of logs for each installed dependency.</returns>
        public async Task<(List<filedata>?, List<VersionData>?, List<LogLine>)> InstallCompleteProject(VersionData projectVersion, Profile defaultProfile, string destDir, string functionCallerNester, string dependencyProgresser)
        {
            HttpClient http = new HttpClient();
            List<filedata> fileDataResults = new List<filedata>();
            List<VersionData> versionDataResults = new List<VersionData>();
            List<LogLine> logs = new List<LogLine>();
            List<filedata> currentFileDataResults;

            Console.WriteLine($"{indexer}Getting information...");
            (ProjectData projectData, LogLine tempLogLine) =
                await ProjectRequester.GetProjectMetadata(http, projectVersion.project_id, $"{functionCallerNester}InstallCompleteProject:");
            logs.Add(tempLogLine);

            if (projectData == null)
            {
                Console.WriteLine($"Could not get information for project ID: {projectVersion.id}. Error message: {tempLogLine.Data}");
                return (null, null, logs);
            }
            projectVersion.related_project_title = projectData.title;
            projectVersion.related_project_description = projectData.description;
            versionDataResults.Add(projectVersion);

            if (installedMods.GetProject(projectVersion.project_id) != null)
            {
                logs.Add(new LogLine
                {
                    Data = $"{functionCallerNester}InstallCompleteProject: PROJECT_id:{projectVersion.project_id}_ALREADY_INSTALLED",
                    IsSuccessful = false,
                    LogLineState = LogState.Info
                });
                Console.WriteLine($"{indexer}{projectData.title} ({projectVersion.name}) is already installed.");
                return (null, null, logs);
            }
            else installedMods.AddProject(DatabaseLinker.ConvertOnlineToLocalProjectVersion(projectVersion));

            Console.WriteLine($"{indexer}Downloading {projectData.title}... ({dependencyProgresser})");
            (currentFileDataResults, List<LogLine> currentLogLineResults) =
                await InstallProjectFiles(projectVersion, http, destDir, true, functionCallerNester + "InstallCompleteProject.");

            logs.AddRange(currentLogLineResults);
            if (currentFileDataResults != null)
                fileDataResults.AddRange(currentFileDataResults);

            indexer = indexer + "  ";
            (currentFileDataResults, List<VersionData> currentVersionDataResults, List<LogLine> currentLogs) =
                await InstallDependencies(projectVersion.dependencies, defaultProfile, http, destDir, functionCallerNester + "InstallCompleteProject.");
            indexer = indexer.Remove(0, 2);

            logs.AddRange(currentLogs);
            if (currentFileDataResults != null && currentVersionDataResults != null)
            {
                fileDataResults.AddRange(currentFileDataResults);
                versionDataResults.AddRange(currentVersionDataResults);
            }
            Console.WriteLine($"{indexer}{projectData.title} downloaded! ({dependencyProgresser})");

            return (fileDataResults, versionDataResults, logs);
        }

        /// <summary>
        /// Downloads the project files.
        /// </summary>
        /// <param name="projectVersionData">The version data of the project to be downloaded</param>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="destDir">The destination directory for the downloaded files</param>
        /// <param name="onlyFirstFile">Whether to download only the first file of the project (recommended since the rest are mostly useless)</param>
        /// <param name="functionCallerNester">Who in the world called me???</param>
        /// <returns>A list of the downloaded files. A list of logs for each downloaded file.</returns>
        public static async Task<(List<filedata>?, List<LogLine>)> InstallProjectFiles(VersionData projectVersionData, HttpClient http, string destDir, bool onlyFirstFile, string functionCallerNester)
        {
            if (onlyFirstFile)
            {
                try
                {
                    projectVersionData.files[0].filename = Path.Combine(destDir, projectVersionData.files[0].filename);
                    byte[] fileContents = await http.GetByteArrayAsync(projectVersionData.files[0].url);
                    File.WriteAllBytes(projectVersionData.files[0].filename, fileContents);
                    return (
                        new List<filedata> { projectVersionData.files[0] },
                        new List<LogLine> {new LogLine
                        {
                            Data = $"{functionCallerNester}InstallProjectFiles: PROJECT_id:{projectVersionData.project_id}.versionID:{projectVersionData.id}.file0_DOWNLOADED",
                            IsSuccessful = true,
                            LogLineState = LogState.Info
                        } });
                }
                catch (Exception e)
                {
                    return (
                        null,
                        new List<LogLine> { new LogLine
                        {
                            Data = $"{functionCallerNester}InstallProjectFiles: PROJECT_id:{projectVersionData.project_id}.versionID:{projectVersionData.id}.file0: {e.Message}",
                            IsSuccessful = false,
                            LogLineState = LogState.Error
                        } });
                }
            }
            else
            {
                List<LogLine> logs = new List<LogLine>();
                List<filedata> resultFileData = new List<filedata>();
                int counter = 0;
                foreach (filedata file in projectVersionData.files)
                {
                    counter++;
                    try
                    {
                        file.filename = Path.Combine(destDir, file.filename);
                        byte[] fileContents = await http.GetByteArrayAsync(projectVersionData.files[0].url);
                        File.WriteAllBytes(file.filename, fileContents);
                        logs.Add(new LogLine
                        {
                            Data = $"{functionCallerNester}InstallProjectFiles: PROJECT_id:{projectVersionData.project_id}.versionID:{projectVersionData.id}.file{counter}_DOWNLOADED",
                            IsSuccessful = true,
                            LogLineState = LogState.Info
                        });
                        resultFileData.Add(file);
                    }
                    catch (Exception e)
                    {
                        logs.Add(new LogLine
                        {
                            Data = $"{functionCallerNester}InstallProjectFiles: PROJECT_id:{projectVersionData.project_id}.versionID:{projectVersionData.id}.file{counter}: {e.Message}",
                            IsSuccessful = false,
                            LogLineState = LogState.Warning
                        });
                    }
                }
                return (resultFileData, logs);
            }
        }

        /// <summary>
        /// Downloads this project's dependencies and their dependencies
        /// </summary>
        /// <param name="dependencies">The dependency list to download</param>
        /// <param name="defaultProfile">The profile to grab the most compatible version of the dependencies and thir dependencies</param>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="dirDest">The destination directory</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A list of filedata and a list of versiondata which represent the newly installed projects. A list of LogLine representing the state of each downloading process.</returns>
        public async Task<(List<filedata>?, List<VersionData>?, List<LogLine>)> InstallDependencies(List<dependency> dependencies, Profile defaultProfile, HttpClient http, string dirDest, string functionCallerNester)
        {
            List<filedata> fileDataResults = new List<filedata>();
            List<VersionData> versionDataResults = new List<VersionData>();
            List<LogLine> logs = new List<LogLine>();
            int counter = 0;

            foreach (dependency dependency in dependencies)
            {
                counter++;
                if (dependency.project_id != null)
                {
                    if (dependency.version_id != null)
                    {
                        (VersionData currentProjectVerData, LogLine logline) =
                            await ProjectRequester.GetProjectSpecificVersion(http, dependency.project_id, dependency.version_id, functionCallerNester + "InstallDependencies.");
                        logs.Add(logline);
                        if (logline.IsSuccessful == true)
                        {
                            (List<filedata> currentFileDataResults, List<VersionData> currentVersionDataResults, List<LogLine> logLines) =
                                await InstallCompleteProject(currentProjectVerData, defaultProfile, dirDest, functionCallerNester + "InstallDependencies.", $"{counter}/{dependencies.Count}");
                            logs.AddRange(logLines);
                            if (currentFileDataResults != null && currentVersionDataResults != null)
                            {
                                versionDataResults.AddRange(currentVersionDataResults);
                                fileDataResults.AddRange(currentFileDataResults);
                            }
                        }
                        else
                            logs.Add(new LogLine
                            {
                                Data = $"{functionCallerNester}InstallDependencies: DEPENDENCY_id:{dependency.project_id}.versionID:{dependency.version_id}_ID_NULL",
                                IsSuccessful = false,
                                LogLineState = LogState.Warning
                            });
                    }
                    else if (dependency.version_id == null && (dependency.dependency_type.Equals("required") || dependency.dependency_type.Equals("embedded")))
                    {
                        (VersionData dependencyVersion, LogLine logLine) =
                            await VersionGrabber.GrabSpecificVersionOnline(http, dependency.project_id, defaultProfile.mcVersion, defaultProfile.loader, null, functionCallerNester + "InstallDependencies.");
                        logs.Add(logLine);
                        if (logLine.LogLineState == LogState.Info)
                        {
                            (List<filedata> currentFileDataResults, List<VersionData> currentVersionDataResults, List<LogLine> logLines) =
                                await InstallCompleteProject(dependencyVersion, defaultProfile, dirDest, functionCallerNester + "InstallDependencies.", $"{counter}/{dependencies.Count}");
                            if (currentFileDataResults != null && currentVersionDataResults != null)
                            {
                                versionDataResults.AddRange(currentVersionDataResults);
                                fileDataResults.AddRange(currentFileDataResults);
                            }
                        }
                    }
                }
            }

            return (fileDataResults, versionDataResults, logs);
        }

        /// <summary>
        /// Gets the newly updated list of installed projects
        /// </summary>
        /// <returns>ProjectDatabase containing the list</returns>
        public ProjectDatabase GetInstalledMods() => installedMods;
    }

    internal class ProjectUpdater
    {
        /// <summary>
        /// Updates all the projects in the provided database list
        /// </summary>
        /// <param name="installedProjects">The database of installed projects to update</param>
        /// <param name="workingProfile">The game profile to grab the best compatible version if a new version is found</param>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>ProjectDatabase containing the updated projects. A list of LogLine representing the state of each installation process</returns>
        public static async Task<(ProjectDatabase, List<LogLine>)> UpdateAllProjects(ProjectDatabase installedProjects, Profile workingProfile, HttpClient http, string functionCallerNester)
        {
            List<LogLine> log = new List<LogLine>();
            List<ProjectVersion> toUpdateModpacks = new List<ProjectVersion>();
            ProjectDatabase updatedMods = new ProjectDatabase(installedProjects.GetConfigFilePath());
            ProjectDatabase updatedResoucepacks = new ProjectDatabase(installedProjects.GetConfigFilePath());
            DirectoryInfo tempDir = Directory.CreateTempSubdirectory("modpack.installer.");
            updatedMods.database.Clear();
            updatedResoucepacks.database.Clear();

            Console.Clear();
            Console.WriteLine("Checking for updates...");
            //We first update all mods and resourcepacks
            for (int i = 0; i < installedProjects.database.Count; i++)
            {
                ProjectVersion currentProject = installedProjects.database[i];
                string modType = Path.GetExtension(currentProject.filePath);
                if (modType.Equals(".mrpack"))
                    toUpdateModpacks.Add(currentProject);
                else if (modType.Equals(".jar"))
                {
                    (VersionData projectData, LogLine tempLogLine) =
                        await VersionGrabber.GrabSpecificVersionOnline(http, currentProject.projectID, workingProfile.mcVersion, workingProfile.loader, null, functionCallerNester + "UpdateAllProjects.");
                    log.Add(tempLogLine);
                    if (projectData.id != currentProject.versionID)
                    {
                        Console.WriteLine($"{currentProject.title} new update found, updating...");
                        projectData.files =
                            ProjectInstaller.InstallProjectFiles(projectData, http, tempDir.FullName, onlyFirstFile: true, functionCallerNester + "UpdateAllProjects.").Result.Item1;
                        File.Delete(currentProject.filePath);
                        File.Move(projectData.files[0].filename, Path.Combine(workingProfile.gameDir, "mods", Path.GetFileName(projectData.files[0].filename)));
                        projectData.files[0].filename = Path.Combine(workingProfile.gameDir, "mods", Path.GetFileName(projectData.files[0].filename));
                        projectData.related_project_title = currentProject.title;
                        projectData.related_project_description = currentProject.description;
                        updatedMods.AddProject(DatabaseLinker.ConvertOnlineToLocalProjectVersion(projectData));
                        updatedMods.database.Last().dependenciesProjectsID = currentProject.dependenciesProjectsID;
                        Console.WriteLine($"{currentProject.title} has been updated successfully!");
                    }
                    else
                    {
                        updatedMods.AddProject(currentProject);
                        Console.WriteLine($"{currentProject.title} is already up to date.");
                    }
                }
                else
                {
                    (VersionData projectData, LogLine tempLogLine) =
                        await VersionGrabber.GrabSpecificVersionOnline(http, currentProject.projectID, workingProfile.mcVersion, "minecraft", null, functionCallerNester + "UpdateAllProjects.");
                    log.Add(tempLogLine);
                    if (projectData.id != currentProject.versionID)
                    {
                        Console.WriteLine($"{currentProject.title} new update found, updating...");
                        projectData.files =
                            ProjectInstaller.InstallProjectFiles(projectData, http, tempDir.FullName, onlyFirstFile: true, functionCallerNester + "UpdateAllProjects.").Result.Item1;
                        File.Delete(currentProject.filePath);
                        File.Move(projectData.files[0].filename, Path.Combine(workingProfile.gameDir, "mods", Path.GetFileName(projectData.files[0].filename)));
                        projectData.files[0].filename = Path.Combine(workingProfile.gameDir, "mods", Path.GetFileName(projectData.files[0].filename));
                        projectData.related_project_title = currentProject.title;
                        projectData.related_project_description = currentProject.description;
                        updatedResoucepacks.AddProject(DatabaseLinker.ConvertOnlineToLocalProjectVersion(projectData));
                        updatedResoucepacks.database.Last().dependenciesProjectsID = currentProject.dependenciesProjectsID;
                        Console.WriteLine($"{currentProject.title} has been updated successfully!");
                    }
                    else
                    {
                        updatedResoucepacks.AddProject(currentProject);
                        Console.WriteLine($"{currentProject.title} is already up to date.");
                    }
                }
            }

            //Then we check with modpacks, if there's a new mod in any modpack's dependencies, we install it
            List<LogLine> tempLogLines;
            bool[] isModNeeded = new bool[updatedMods.database.Count];
            if (toUpdateModpacks.Count > 0)
            {
                foreach (ProjectVersion currentModpack in toUpdateModpacks)
                {
                    (VersionData projectData, LogLine tempLogLine) =
                        await VersionGrabber.GrabSpecificVersionOnline(http, currentModpack.projectID, workingProfile.mcVersion, workingProfile.loader, null, functionCallerNester + "UpdateAllProjects.");
                    log.Add(tempLogLine);
                    List<string> updatedDependenciesID = new List<string>();
                    foreach (dependency dependency in projectData.dependencies)
                    {
                        if (dependency.dependency_type.Equals("required") || dependency.dependency_type.Equals("embedded"))
                            updatedDependenciesID.Add(dependency.project_id);
                    }

                    if (projectData != null && !projectData.version_number.Equals(currentModpack.versionName))
                    {
                        Console.WriteLine($"{currentModpack.title} new update found, updating...");
                        foreach (string dependencyFromUpdatedDpendencies in updatedDependenciesID)
                            if (updatedMods.GetProject(dependencyFromUpdatedDpendencies) == null)
                            {
                                (VersionData modData, tempLogLine) =
                                    await VersionGrabber.GrabSpecificVersionOnline(http, dependencyFromUpdatedDpendencies, workingProfile.mcVersion, workingProfile.loader, null, functionCallerNester + "UpdateAllProjects.");
                                log.Add(tempLogLine);
                                ProjectInstaller installer = new ProjectInstaller(updatedMods);
                                (List<filedata> newInstalledFiles, List<VersionData> newInstalledMods, tempLogLines) =
                                    await installer.InstallCompleteProject(modData, workingProfile, tempDir.FullName, functionCallerNester + "UpdateAllProjects.", string.Empty);

                                foreach (VersionData newInstalledMod in newInstalledMods)
                                    updatedMods.AddProject(DatabaseLinker.ConvertOnlineToLocalProjectVersion(newInstalledMod));
                            }

                        (projectData.files, tempLogLines) =
                            await ProjectInstaller.InstallProjectFiles(projectData, http, tempDir.FullName, onlyFirstFile: true, $"{functionCallerNester}UpdateAllProjects.");
                        log.AddRange(tempLogLines);
                        if (projectData.files != null)
                        {
                            ZipFile.ExtractToDirectory(projectData.files[0].filename, tempDir.FullName);
                            File.Delete(projectData.files[0].filename);
                            string tempConfigFolderPath = Path.Combine(tempDir.FullName, "overrides", "config");
                            string gameConfigFolderPath = Path.Combine(workingProfile.gameDir, "config");
                            bool shouldReplace = false;
                            if (currentModpack.shouldReplaceConfigs == "true") shouldReplace = true;
                            else if (currentModpack.shouldReplaceConfigs == "ask")
                            {
                                string message = $"Let {currentModpack.title} change your mods settings?\n(recommended for optimization modpacks)";
                                shouldReplace = PromptUserAnswer.YorNAnswer(message);
                            }

                            Functions.MoveConfigFiles(tempConfigFolderPath, gameConfigFolderPath, shouldReplace);
                        }

                        Console.WriteLine($"{currentModpack.title} has been updated successfully!");
                    }
                    else Console.WriteLine($"{currentModpack.title} is already up to date.");
                    updatedMods.AddProject(currentModpack);
                }
            }

            updatedMods.database.AddRange(updatedResoucepacks.database);
            updatedMods.Save();
            tempDir.Delete(recursive: true);
            return (updatedMods, log);
        }
    }
}
