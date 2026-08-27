using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.ProjectDatabaseManager;
using System.Runtime.CompilerServices;
using System.Security.Cryptography;
using System.Text;

namespace Modpack_Installer.ConfigManager
{
    /// <summary>
    /// This class provides functions for managing local project database
    /// </summary>
    public class ProjectDatabase
    {
        public List<ProjectVersion> database = new List<ProjectVersion>();
        string configFilePath;

        //It automatically reads the saved projects
        public ProjectDatabase(string configFilePath)
        {
            if (File.Exists(configFilePath))
                database = ReadAllProjectVersions(configFilePath);
            this.configFilePath = configFilePath;
        }

        /// <summary>
        /// Reads all the projects from the config file
        /// </summary>
        /// <param name="configFilePath">The path of the config file to read from</param>
        private List<ProjectVersion> ReadAllProjectVersions(string configFilePath)
        {
            List<ProjectVersion> resultProjectVersions = new List<ProjectVersion>();
            StreamReader reader = new StreamReader(configFilePath);

            while (reader.Peek() == '{')
            {
                ProjectVersion currentProjectVersion = new ProjectVersion();

                string currentLine = reader.ReadLine();
                string[] parameters = currentLine.Split("\",\"");

                currentProjectVersion.title = parameters[0].Substring(2);
                currentProjectVersion.description = parameters[1].Replace("\\n", "\n");
                currentProjectVersion.projectID = parameters[2];
                currentProjectVersion.versionID = parameters[3];
                currentProjectVersion.versionName = parameters[4];
                currentProjectVersion.versionType = parameters[5];
                currentProjectVersion.filePath = parameters[6];
                currentProjectVersion.shouldReplaceConfigs = parameters[7];

                string[] splittedArray = parameters[8].Substring(1, parameters[8].Length - 2).Split(',');
                currentProjectVersion.mcVersions = splittedArray.ToList();

                splittedArray = parameters[9].Substring(1, parameters[9].Length - 2).Split(',');
                currentProjectVersion.loaders = splittedArray.ToList();

                splittedArray = parameters[10].Substring(1, parameters[10].Length - 4).Split(',', StringSplitOptions.RemoveEmptyEntries);
                currentProjectVersion.dependenciesProjectsID = splittedArray.ToList();


                resultProjectVersions.Add(currentProjectVersion);
            }

            reader.Close();
            return resultProjectVersions;
        }

        /// <summary>
        /// Adds a project to the database
        /// </summary>
        /// <param name="projectVersion">The project local data to be added</param>
        public void AddProject(ProjectVersion projectVersion) => database.Add(projectVersion);
        
        /// <summary>
        /// Removes a project from the database
        /// </summary>
        /// <param name="projectID">The project ID which corresponds to the desired project to be removed</param>
        public void RemoveProject(string projectID)
        {
            foreach (ProjectVersion projectVersion in database)
                if (projectVersion.projectID == projectID)
                {
                    database.Remove(projectVersion);
                    break;
                }
        }

        /// <summary>
        /// Saves the database to the config file
        /// </summary>
        public void Save()
        {
            StringBuilder configFileResult = new StringBuilder();
            int counter = 0;
            foreach (ProjectVersion projectVersion in database)
            {
                string projectConfigLine = "{" + $"\"{projectVersion.title}\",\"{projectVersion.description.Replace("\n", "\\n")}\"," +
                    $"\"{projectVersion.projectID}\",\"{projectVersion.versionID}\",\"{projectVersion.versionName}\"," +
                    $"\"{projectVersion.versionType}\",\"{projectVersion.filePath}\"," +
                    $"\"{(projectVersion.shouldReplaceConfigs == string.Empty ? "ask" : projectVersion.shouldReplaceConfigs)}\"," +
                    $"\"[{string.Join(',', projectVersion.mcVersions)}]\",\"[{string.Join(',', projectVersion.loaders)}]\"," +
                    $"\"[{string.Join(',', projectVersion.dependenciesProjectsID)}]\"" + "}";

                configFileResult.AppendLine(projectConfigLine);
            }
            File.WriteAllText(configFilePath, configFileResult.ToString());
        }

        /// <summary>
        /// Gets the specified project by its index
        /// </summary>
        /// <param name="index"></param>
        /// <returns>A ProjectVersion if the index is within the range, otherwise null</returns>
        public ProjectVersion? GetProject(int index)
        {
            try { return database[index]; }
            catch { return null; }
        }
        /// <summary>
        /// Gets the specified project by its project ID
        /// </summary>
        /// <param name="index"></param>
        /// <returns>A ProjectVersion if the corresponding project ID is found, otherwise null</returns>
        public ProjectVersion? GetProject(string projectID)
        {
            foreach (ProjectVersion projectVersion in database)
                if (projectVersion.projectID == projectID)
                    return projectVersion;
            return null;
        }

        /// <summary>
        /// Gets the config file path
        /// </summary>
        /// <returns>A string which is a path to the working database file</returns>
        public string GetConfigFilePath() => configFilePath;
    }


    /// <summary>
    /// It links between the local database and the online database... yknow...
    /// </summary>
    internal class DatabaseLinker
    {
        /// <summary>
        /// Converts the online project metadata to local project metadata (saving only whats necessary)
        /// </summary>
        /// <param name="versionData">The VersionData to convert from</param>
        /// <returns>A ProjectVersion which is converted from the VersionData</returns>
        public static ProjectVersion ConvertOnlineToLocalProjectVersion(VersionData versionData)
        {
            ProjectVersion resultProjectVersion = new ProjectVersion
            {
                title = versionData.related_project_title,
                description = versionData.related_project_description.Replace("\n", "\\n"),
                projectID = versionData.project_id,
                versionID = versionData.id,
                filePath = versionData.files[0].filename,
                versionType = versionData.version_type,
                mcVersions = versionData.game_versions,
                loaders = versionData.loaders,
                versionName = versionData.version_number
            };
            foreach (dependency dependency in versionData.dependencies)
                resultProjectVersion.dependenciesProjectsID.Add(dependency.project_id);

            return resultProjectVersion;
        }

        /// <summary>
        /// Converts the local project metadata to online project metadata (making sure the data is updated)
        /// </summary>
        /// <param name="projectVersion">The local project to convert from</param>
        /// <param name="http">To evade multiple instances from the same HttpClient class</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>The VersionData of the project, or null if not found. The ProjectData of the project, or null if not found. A LogLine representing the state of the operation.</returns>
        public static async Task<(VersionData?, ProjectData?, LogLine)> ConvertLocalToOnlineProjectVersion(ProjectVersion projectVersion, HttpClient http, string functionCallerNester)
        {
            LogLine log;

            (VersionData versionData, log) = await ProjectRequester.GetProjectSpecificVersion(http, projectVersion.projectID, projectVersion.versionID, functionCallerNester + "ConvertLocalToOnlineProjectVersion.");
            if (versionData == null) return (null, null, log);
            (ProjectData projectData, log) = await ProjectRequester.GetProjectMetadata(http, projectVersion.projectID, functionCallerNester + "ConvertLocalToOnlineProjectVersion.");
            if (projectData == null) return (versionData, null, log);

            return (versionData, projectData, new LogLine
            {
                Data = $"{functionCallerNester}ConvertLocalToOnlineProjectVersion: LOCAL_TO_ONLINE_projectID:{projectVersion.projectID}_SUCCESS",
                IsSuccessful = true,
                LogLineState = LogState.Info
            });
        }
    }
}
