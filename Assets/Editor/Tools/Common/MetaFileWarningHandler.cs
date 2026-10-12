using UnityEditor;
using UnityEngine;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Com.Udogames.Core.Editor.Tools.Common
{
  // if you want to clean those "A meta data file X exists but its asset X can't be found" errors uncomment this
  // [InitializeOnLoad]
  public static class MetaFileWarningHandler
  {
    // Regex to match the path inside the Unity warning
    private static readonly Regex MetaWarningRegex = new Regex(
      @"A meta data file \(.*\.meta\) exists but its asset '(.+?)' can't be found",
      RegexOptions.Compiled
    );

    static MetaFileWarningHandler()
    {
      Application.logMessageReceived += HandleLogMessage;
    }

    private static void HandleLogMessage(string condition, string stackTrace, LogType type)
    {
      if (type != LogType.Warning) return;

      Match match = MetaWarningRegex.Match(condition);
      if (match.Success)
      {
        string metaFile = GetPhysicalPath(match.Groups[1].Value + ".meta");
        if (metaFile == null)
          return;
        string assetFile = GetPhysicalPath(match.Groups[1].Value);
        if (assetFile == null)
          return;


        if (!File.Exists(assetFile) && !Directory.Exists(assetFile))
        {
          try
          {
            File.Delete(metaFile);
            // deletedCount++;
            Debug.Log($"[MetaCleaner] Deleted orphaned meta: {metaFile}");
          }
          catch (IOException ex)
          {
            Debug.LogWarning($"[MetaCleaner] Failed to delete {metaFile}: {ex.Message}");
          }
        }
      }
    }

    public static string GetPhysicalPath(string packageRelativePath)
    {
      if (string.IsNullOrEmpty(packageRelativePath))
      {
        Debug.LogWarning("[MetaCleaner] Invalid input: path is null or empty.");
        return null;
      }

      string packageSubPath = packageRelativePath;
      if (packageSubPath.StartsWith("Packages/"))
      {
        packageSubPath = packageSubPath.Substring("Packages/".Length);
      }

      // 1. Check embedded packages folder
      string embeddedPath = Path.GetFullPath(Path.Combine(Application.dataPath, "../Packages", packageSubPath));
      if (File.Exists(embeddedPath) || Directory.Exists(embeddedPath))
      {
        Debug.Log("[MetaCleaner] Found embedded path: " + embeddedPath);
        return embeddedPath;
      }

      // 2. Check Library/PackageCache folder
      string cacheRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Library/PackageCache"));

      if (!Directory.Exists(cacheRoot))
      {
        Debug.LogWarning("[MetaCleaner] PackageCache folder does not exist: " + cacheRoot);
        return null;
      }

      string packageName = packageSubPath.Split('/')[0];
      if (string.IsNullOrEmpty(packageName))
      {
        Debug.LogWarning("[MetaCleaner] Invalid package name extracted from: " + packageRelativePath);
        return null;
      }

      string[] packageDirs;
      try
      {
        packageDirs = Directory.GetDirectories(cacheRoot, packageName + "*");
      }
      catch (System.Exception ex)
      {
        Debug.LogError("[MetaCleaner] Error reading package cache: " + ex.Message);
        return null;
      }

      foreach (var dir in packageDirs)
      {
        string innerPath = string.Join("/", packageSubPath.Split('/').Skip(1));
        string candidate = Path.Combine(dir, innerPath);
        Debug.Log("[MetaCleaner] Candidate path: " + candidate);
        return Path.GetFullPath(candidate);
      }

      Debug.LogWarning("[MetaCleaner] Could not resolve package path: " + packageRelativePath);
      return null;
    }
  }
}