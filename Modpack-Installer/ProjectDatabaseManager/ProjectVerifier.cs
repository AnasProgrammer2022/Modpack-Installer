using Modpack_Installer.Definitions.JsonRequestDefinitions;
using Modpack_Installer.Definitions.LocalDefinitions;
using Modpack_Installer.Definitions.OnlineDefinitions;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;

namespace Modpack_Installer.InstallerAndVerifier
{
    internal class ProjectVerifier
    {
        /// <summary>
        /// Is the downloaded file is complete without corruptions?
        /// </summary>
        /// <param name="file">The file with its expected correct data to verify</param>
        /// <param name="functionCallerNester">Who called me?</param>
        /// <returns>A LogLine which its state is Info if its correct, otherwise Error with the data providing the reason</returns>
        public static LogLine IsFileCorrect(filedata file, string functionCallerNester)
        {
            if (!File.Exists(file.filename))
                return new LogLine
                {
                    Data = $"{functionCallerNester}IsFileCorrect: FILE_filename:{file.filename}_NOT_FOUND",
                    IsSuccessful = true,
                    LogLineState = LogState.Error
                };

            if (File.ReadAllBytes(file.filename).Length != file.size)
                return new LogLine
                {
                    Data = $"{functionCallerNester}IsFileCorrect: FILE_filename:{file.filename}_SIZE_NOT_MATCH",
                    IsSuccessful = true,
                    LogLineState = LogState.Error
                };

            byte[] fileContents = File.ReadAllBytes(file.filename);
            byte[] fileSHA1 = SHA1.HashData(fileContents);
            byte[] fileSHA512 = SHA512.HashData(fileContents);

            if (Convert.ToHexStringLower(fileSHA1) != file.hashes.sha1 || Convert.ToHexStringLower(fileSHA512) != file.hashes.sha512)
                return new LogLine
                {
                    Data = $"{functionCallerNester}IsFileCorrect: FILE_filename:{file.filename}_CONTENTS_NOT_MATCH",
                    IsSuccessful = true,
                    LogLineState = LogState.Error
                };

            return new LogLine
            {
                Data = $"{functionCallerNester}IsFileCorrect: FILE_filename:{file.filename}_IS_CORRECT",
                IsSuccessful = true,
                LogLineState = LogState.Info
            };
        }

        /// <summary>
        /// (before using ProjectDatabase as a database) Is this project already installed before.
        /// </summary>
        /// <param name="installedProjects">The list of already installed projects</param>
        /// <param name="project">The project to compare if it already exists</param>
        /// <param name="sameVersion">Should the function also compare the version? (even if older)</param>
        /// <returns>True if the project is already installed, otherwise false</returns>
        public static bool IsProjectInstalled(List<VersionData> installedProjects, VersionData project, bool sameVersion)
        {
            bool isSameProjectID = false;
            bool isSamePublishDate = false;

            if (installedProjects == null) return false;
            foreach (VersionData currentInsProject in installedProjects)
            {
                if (currentInsProject.project_id == project.project_id) isSameProjectID = true;
                if (currentInsProject.date_published == project.date_published) isSamePublishDate = true;

                if (isSameProjectID)
                {
                    if (sameVersion)
                        if (isSamePublishDate) return true;
                        else return false;
                    else return true;
                }
                else { isSameProjectID = false; isSamePublishDate = false; }
            }

            return false;
            
        }

        /// <summary>
        /// (Tbh I wrote this function earlier in development and I've never used it... now I forgot what its purpose)
        /// </summary>
        /// <param name="project"></param>
        /// <param name="desiredMCVersion"></param>
        /// <param name="desiredLoader"></param>
        /// <param name="desiredVersionType"></param>
        /// <exception cref="Exception"></exception>
        public static void VersionInProject(ProjectData project, string desiredMCVersion, string desiredLoader, string desiredVersionType)
        {
            if (project == null) throw new Exception("PROJECT_NOT_EXIST");
            bool hasDMCVersion = false, hasDLoader = false, hasDVerType = false;
            foreach (string game_version in project.game_versions)
                if (game_version == desiredMCVersion) hasDMCVersion = true;
            foreach (string loader in project.loaders)
                if (loader == desiredLoader) hasDLoader = true;
        }

        /// <summary>
        /// Does the project supports the desired profile?
        /// </summary>
        /// <param name="profile">The desired profile to verify for</param>
        /// <param name="projectData">The project to verify from (provided by search results)</param>
        /// <returns></returns>
        public static bool IsProjectSupportsProfile(Profile profile, SearchHit projectData)
        {
            if (projectData.project_type != "resourcepack")
            {
                bool hasMCVersion = projectData.versions.Any(ver => ver == profile.mcVersion);
                bool hasLoader = projectData.categories.Any(project => project == profile.loader);
                return hasLoader && hasMCVersion;
            }
            else
                return projectData.versions.Any(ver => ver == profile.mcVersion);
        }
    }
}
