using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.Definitions.OnlineDefinitions;
using System.Net.Http.Json;

namespace Modpack_Installer.ProjectDatabaseManager
{
    internal class ProjectRequester
    {
        /// <summary>
        /// Gets a list of avilable project's versions from Modrinth database
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectIDorSlug">The project ID to get its versions</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A list of a project's versions, or null if not found. A LogLine which represents the state of the process.</returns>
        public static async Task<(List<VersionData>?, LogLine)> GetProjectVersions(HttpClient http, string projectIDorSlug, string functionCallerNester)
        {
            string requestURL = $"https://api.modrinth.com/v2/project/{projectIDorSlug}/version";
            try
            {
                List<VersionData>? versions = await http.GetFromJsonAsync<List<VersionData>>(requestURL);
                return (versions, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectVersions: HTTP_REQUEST_projectID:{projectIDorSlug}_SUCCESS",
                    IsSuccessful = true,
                    LogLineState = LogState.Info
                });
            }
            catch (Exception e)
            {
                return (null, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectVersions: {e.Message}",
                    IsSuccessful = false,
                    LogLineState = LogState.Error
                });
            }
        }

        /// <summary>
        /// Gets a project version with a provided version ID from Modrinth online database
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectIDorSlug">The project ID to get its version</param>
        /// <param name="versionID">The version ID of the desired version</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A VersionData containing the data of the desired version, or null if not found. A LogLine representing the state of the process.</returns>
        public static async Task<(VersionData?, LogLine)> GetProjectSpecificVersion(HttpClient http, string projectIDorSlug, string versionID, string functionCallerNester)
        {
            string requestURL = $"https://api.modrinth.com/v2/project/{projectIDorSlug}/version/{versionID}";
            try
            {
                VersionData? versions = await http.GetFromJsonAsync<VersionData>(requestURL);
                return (versions, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectSpecificVersion: HTTP_REQUEST_projectID:{projectIDorSlug}.versionID:{versionID}_SUCCESS",
                    IsSuccessful = true,
                    LogLineState = LogState.Info
                });
            }
            catch (Exception e)
            {
                return (null, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectSpecificVersion: {e.Message}",
                    IsSuccessful = false,
                    LogLineState = LogState.Error
                });
            }
        }

        /// <summary>
        /// Gets a project metadata (title, description, author...) from Modrinth online database
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectIDorSlug">The project ID to get its metadata</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A ProjectData containing the metadata of the project, or null if not found. A LogLine representing the state of the process.</returns>
        public static async Task<(ProjectData?, LogLine)> GetProjectMetadata(HttpClient http, string projectIDorSlug, string functionCallerNester)
        {
            string requestURL = $"https://api.modrinth.com/v2/project/{projectIDorSlug}";
            try
            {
                ProjectData? data = await http.GetFromJsonAsync<ProjectData>(requestURL);
                return (data, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectMetadata: HTTP_REQUEST_projectID:{projectIDorSlug}_SUCCESS",
                    IsSuccessful = true,
                    LogLineState = LogState.Info
                });
            }
            catch (Exception e)
            {
                return (null, new LogLine
                {
                    Data = $"{functionCallerNester}GetProjectMetadata: {e.Message}",
                    IsSuccessful = false,
                    LogLineState = LogState.Error
                });
            }
        }
    }

    internal class VersionGrabber
    {
        /// <summary>
        /// Gets the desired version from the provided VersionData list depnding on the game version, the mod loader and the version type (release, beta or alpha)
        /// </summary>
        /// <param name="versionData">The list of VersionData to search in</param>
        /// <param name="desiredGameVer">The desired game version</param>
        /// <param name="desiredLoader">The desired mod loader</param>
        /// <param name="desiredVerType">The desired version type</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A VerionData if a version matching the desired is found, or null if not. A LogLine representing the state of the process.</returns>
        public static (VersionData?, LogLine) GrabSpecificVersion(List<VersionData> versionData, string desiredGameVer, string desiredLoader, string desiredVerType, string functionCallerNester)
        {
            bool hasDesiredGameVer = false, hasDesiredLoader = false;
            foreach (VersionData currentVersionData in versionData)
            {
                hasDesiredGameVer = currentVersionData.game_versions.Any(ver => ver == desiredGameVer);
                hasDesiredLoader = currentVersionData.loaders.Any(ver => ver == desiredLoader);
                if ((desiredVerType == null ? true : currentVersionData.version_type == desiredVerType) && hasDesiredGameVer && hasDesiredLoader)
                    return (currentVersionData, new LogLine
                    {
                        Data = $"{functionCallerNester}GrabSpecificVersion: VERSION_projectID:{currentVersionData.project_id}.versionID:{currentVersionData.id}_FOUND",
                        IsSuccessful = true,
                        LogLineState = LogState.Info
                    });
                else { hasDesiredLoader = false; hasDesiredGameVer = false; }
            }
            return (null, new LogLine
            {
                Data = $"{functionCallerNester}GrabSpecificVersion: DESIRED_VERSION_projectID:{versionData[0].project_id}_NOT_FOUND",
                IsSuccessful = true,
                LogLineState = LogState.Error
            });
        }
        /// <summary>
        /// Gets the desired version from the provided VersionData list depnding on the game version list, the mod loader list and the version type (note: if one element of the desired game versions and the desired loader match, then the version will be returned)
        /// </summary>
        /// <param name="versionData">The list of VersionData to search in</param>
        /// <param name="desiredGameVers">The desired game version</param>
        /// <param name="desiredLoaders">The desired mod loader</param>
        /// <param name="desiredVerType">The desired version type</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A VerionData if a version matches the desired is found, or null if not. A LogLine representing the state of the process.</returns>
        public static (VersionData?, LogLine) GrabSpecificVersion(List<VersionData> versionData, List<string> desiredGameVers, List<string> desiredLoaders, string desiredVerType, string functionCallerNester)
        {
            bool hasDesiredGameVer = false, hasDesiredLoader = false;
            foreach (VersionData currentVersionData in versionData)
            {
                hasDesiredGameVer = currentVersionData.game_versions.Any(ver => desiredGameVers.Any(desVer => desVer == ver));
                hasDesiredLoader = currentVersionData.loaders.Any(ver => desiredLoaders.Any(desVer => desVer == ver));
                if (currentVersionData.version_type == desiredVerType && hasDesiredGameVer && hasDesiredLoader)
                    return (currentVersionData, new LogLine
                    {
                        Data = $"{functionCallerNester}GrabSpecificVersion[]: VERSION_projectID:{currentVersionData.project_id}.versionID:{currentVersionData.id}_FOUND",
                        IsSuccessful = true,
                        LogLineState = LogState.Info
                    });
                else { hasDesiredLoader = false; hasDesiredGameVer = false; }
            }
            return (null, new LogLine
            {
                Data = $"{functionCallerNester}GrabSpecificVersion[]: DESIRED_VERSION_projectID:{versionData[0].project_id}_NOT_FOUND",
                IsSuccessful = true,
                LogLineState = LogState.Error
            });
        }

        /// <summary>
        /// Gets the desired project version straight from Modrinth online database depending on the desired game version, the mod loader and version type
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectIDorSlug">The project ID to get its version</param>
        /// <param name="desiredGameVer">The desired game version</param>
        /// <param name="desiredLoader">The desired mod loader</param>
        /// <param name="desiredVerType">The desired version type</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A VersionData if a version matches the desire is found, or null if not. A LogLine representing the state of the process.</returns>
        public static async Task<(VersionData?, LogLine)> GrabSpecificVersionOnline(HttpClient http, string projectIDorSlug, string desiredGameVer, string desiredLoader, string desiredVerType, string functionCallerNester)
        {
            LogLine currentLogLine = new LogLine();
            (List<VersionData> projectVersions, currentLogLine) = await ProjectRequester.GetProjectVersions(http, projectIDorSlug, functionCallerNester + "GrabSpecificVersionOnline.");
            if (projectVersions == null) return (null, currentLogLine);
            (VersionData desiredVersionData, currentLogLine) = GrabSpecificVersion(projectVersions, desiredGameVer, desiredLoader, desiredVerType, functionCallerNester + "GrabSpecificVersionOnline.");
            if (desiredVersionData == null) return (null, currentLogLine);
            return (desiredVersionData, new LogLine
            {
                Data = $"GrabSpecificVersionOnline: VERSION_projectID:{projectIDorSlug}.versionID:{desiredVersionData.id}_FOUND",
                IsSuccessful = true,
                LogLineState = LogState.Info
            });
        }

        /// <summary>
        /// Gets the desired project version straight from Modrinth online database depending on the desired game version list, the mod loader list and version type (note: if one element of the desired game versions and the desired loader match, then the version will be returned)
        /// </summary>
        /// <param name="http">To evade multiple instances of the same HttpClient class</param>
        /// <param name="projectIDorSlug">The project ID to get its version</param>
        /// <param name="desiredGameVer">The desired game version</param>
        /// <param name="desiredLoader">The desired mod loader</param>
        /// <param name="desiredVerType">The desired version type</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A VersionData if a version matches the desire is found, or null if not. A LogLine representing the state of the process.</returns>
        public static async Task<(VersionData?, LogLine)> GrabSpecificVersionOnline(HttpClient http, string projectIDorSlug, List<string> desiredGameVer, List<string> desiredLoader, string desiredVerType, string functionCallerNester)
        {
            LogLine currentLogLine = new LogLine();
            (List<VersionData> projectVersions, currentLogLine) = await ProjectRequester.GetProjectVersions(http, projectIDorSlug, functionCallerNester + "GrabSpecificVersionOnline[].");
            if (projectVersions == null) return (null, currentLogLine);
            (VersionData desiredVersionData, currentLogLine) = GrabSpecificVersion(projectVersions, desiredGameVer, desiredLoader, desiredVerType, functionCallerNester + "GrabSpecificVersionOnline[].");
            if (desiredVersionData == null) return (null, currentLogLine);
            return (desiredVersionData, new LogLine
            {
                Data = $"GrabSpecificVersionOnline: VERSION_projectID:{projectIDorSlug}.versionID:{desiredVersionData.id}_FOUND",
                IsSuccessful = true,
                LogLineState = LogState.Info
            });
        }

        /// <summary>
        /// Gets the mod loader and game version from profile lastVersionId property
        /// </summary>
        /// <param name="profile">The profile to grab the data from</param>
        /// <param name="minecraftVersions">The list of released Minecraft versions</param>
        /// <returns></returns>
        public static Profile GrabMinecraftVersionFromProfile(Profile profile, List<MinecraftVersion> minecraftVersions)
        {
            foreach (MinecraftVersion version in minecraftVersions)
                if (profile.lastVersionId.Contains(version.version))
                { profile.mcVersion = version.version; break; }

            if (profile.lastVersionId.Contains("fabric")) profile.loader = "fabric";
            else if (profile.lastVersionId.Contains("neoforge")) profile.loader = "neoforge";
            else if (profile.lastVersionId.Contains("quilt")) profile.loader = "quilt";
            else if (profile.lastVersionId.Contains("forge")) profile.loader = "forge";
            else profile.loader = string.Empty;

            return profile;
        }
    }
}
