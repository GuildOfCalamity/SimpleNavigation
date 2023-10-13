using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Windows.Management.Deployment;

namespace SimpleNavigation;

/// <summary>
/// We'll just use strings for all of the props so we can do some easy formatting.
/// </summary>
public class PackageDetail
{
    public string? Name { get; set; }
    public string? FullName { get; set; }
    public string? Location { get; set; }
    public string? FamilyName { get; set; }
    public string? Architecture { get; set; }
    public string? Version { get; set; }
    public string? ResourcePackage { get; set; }
    public string? Bundled { get; set; }
    public string? Framework { get; set; }
    public string? Disabled { get; set; }
    public string? Dependencies { get; set; }

    /// <summary>LINQ helper.</summary>
    /// <example>_items.Where(o => ApplyFilter(o, "some filter"))</example>
    public bool ApplyFilter(string filter)
    {
        if (!string.IsNullOrEmpty(FullName))
            return FullName.Contains(filter, StringComparison.InvariantCultureIgnoreCase);
        else
            return true;
    }
}

public static class PackageDetailHelper
{
    public static async Task<List<PackageDetail>> GatherAllPackagesForUserAsync(bool debugDump, CancellationToken token = default)
    {
        return await Task.Run(() => { return GatherAllPackagesForUser(debugDump, token).OrderBy(o => o.FullName).ToList(); });
    }

    public static List<PackageDetail> GatherAllPackagesForUser(bool debugDump, CancellationToken token = default)
    {
        List<PackageDetail> items = new();
        PackageManager packageManager = new PackageManager();
        var packages = packageManager.FindPackagesForUser(string.Empty);
        foreach (var package in packages)
        {
            if (token.IsCancellationRequested)
                break;

            string locus = "N/A";
            try
            {
                var sf = package.InstalledLocation;
                locus = $"{sf.Path}";
            }
            catch (Exception) { }

            items.Add(new PackageDetail
            {
                Name = $"{package.Id.Name}",
                FullName = $"{package.Id.FullName}",
                Location = $"{locus}",
                FamilyName = $"Family: {package.Id.FamilyName}",
                Architecture = $"Architecture: {package.Id.Architecture}",
                Version = $"v{package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}",
                Bundled = $"Bundled: {package.IsBundle}",
                Framework = $"Framework: {package.IsFramework}",
                Disabled = $"Disabled: {package.Status.Disabled}",
                ResourcePackage = $"Resource: {package.IsResourcePackage}",
                Dependencies = $"Dependencies: {package.Dependencies.Count}",
            });

            if (debugDump)
            {
                Debug.WriteLine($"Name.............: {package.Id.Name}");
                Debug.WriteLine($"FullName.........: {package.Id.FullName}");
                Debug.WriteLine($"FamilyName.......: {package.Id.FamilyName}");
                Debug.WriteLine($"Architecture.....: {package.Id.Architecture}");
                Debug.WriteLine($"Publisher........: {package.Id.Publisher}");
                Debug.WriteLine($"PublisherID......: {package.Id.PublisherId}");
                Debug.WriteLine($"Version..........: {package.Id.Version.Major}.{package.Id.Version.Minor}.{package.Id.Version.Build}.{package.Id.Version.Revision}");
                Debug.WriteLine($"Disabled?........: {package.Status.Disabled}");
                Debug.WriteLine($"IsFramework......: {package.IsFramework}");
                Debug.WriteLine($"IsResourcePackage: {package.IsResourcePackage}");
                Debug.WriteLine($"IsBundle.........: {package.IsBundle}");
                Debug.WriteLine($"IsDevMode........: {package.IsDevelopmentMode}");
                Debug.WriteLine($"InstalledLocation: {locus}");
                Debug.WriteLine($"Dependencies.....: {package.Dependencies.Count}");
                foreach (var dep in package.Dependencies) { Debug.WriteLine($"  - {dep.Id.FullName}"); }
                Debug.WriteLine("--------------------------------------------------------------");
            }
        }
        return items;
    }
}
