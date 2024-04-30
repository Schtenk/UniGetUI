﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using UniGetUI.Core.Logging;
using UniGetUI.PackageEngine.PackageClasses;

namespace UniGetUI.PackageEngine.Managers.Generic.NuGet.Internal
{
    internal static class PackageManifestLoader
    {
        private static Dictionary<Uri, string> __manifest_cache = new();

        /// <summary>
        /// Returns the URL to the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetPackageManifestUrl(Package package)
        {
            return new Uri($"{package.Source.Url}/Packages(Id='{package.Id}',Version='{package.Version}')");
        }

        /// <summary>
        /// Returns the URL to the NuPk file
        /// </summary>
        /// <param name="package">A valid Package object</param>
        /// <returns>A Uri object</returns>
        public static Uri GetPackageNuGetPackageUrl(Package package)
        {
            return new Uri($"{package.Source.Url}/Packages/{package.Id}.{package.Version}.nupkg");
        }

        /// <summary>
        /// Returns the contents of the manifest of a NuGet-based package
        /// </summary>
        /// <param name="package">The package for which to obtain the manifest</param>
        /// <returns>A string containing the contents of the manifest</returns>
        public static async Task<string?> GetPackageManifestContent(Package package)
        {
            string? PackageManifestContent = "";
            Uri PackageManifestUrl = GetPackageManifestUrl(package);
            if (__manifest_cache.TryGetValue(PackageManifestUrl, out PackageManifestContent) && PackageManifestContent != null)
            {
                Logger.Debug($"Loading cached NuGet manifest for package {package.Id} on manager {package.Manager.Name}");
                return PackageManifestContent;
            }

            try
            {
                HttpClientHandler handler = new HttpClientHandler()
                {
                    AutomaticDecompression = DecompressionMethods.All
                };

                using (HttpClient client = new HttpClient(handler))
                {
                    var response = await client.GetAsync(PackageManifestUrl);
                    if (!response.IsSuccessStatusCode && package.Version.EndsWith(".0"))
                        response = await client.GetAsync(new Uri(PackageManifestUrl.ToString().Replace(".0')", "')")));

                    if (!response.IsSuccessStatusCode)
                    {
                        Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()} with status code {response.StatusCode}");
                        return null;
                    }

                    PackageManifestContent = await response.Content.ReadAsStringAsync();
                }
                return PackageManifestContent;
            }
            catch (Exception e)
            {
                Logger.Warn($"Failed to download the {package.Manager.Name} manifest at Url={PackageManifestUrl.ToString()}");
                Logger.Warn(e);
                return null;
            }
        }
    }
}
