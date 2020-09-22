﻿using System;
using System.Collections.Generic;

namespace ConsoleGPlayAPITool
{
    using System.Linq;
    using UnityEditor;
    using UnityEngine;

    [Serializable]
    public class AndroidDistributionSettings : IAndroidDistributionSettings
    {
        public const string AppBundleExtension = ".aab";

        public static readonly List<string> BranchVersions = new List<string>()
        {
            "internal", "alpha", "beta", "production"
        };

        public static readonly List<string> TrackNameStatus = new List<string>()
        {
            "inProgress", "completed", "draft"
        };

        public string packageName;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FilePath]
        [Sirenix.OdinInspector.Required]
#endif
        public string jsonKeyPath;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.FilePath]
#endif
        public string artifactPath;

        public string recentChangedLang = "en";

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.MultiLineProperty(Lines = 5)]
#endif
        public string recentChangesText;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ValueDropdown(nameof(GetBranchType))]
#endif
        public string trackBranch = BranchVersions.FirstOrDefault();

        public string releaseName;

#if ODIN_INSPECTOR
        [Sirenix.OdinInspector.ValueDropdown(nameof(GetTrackStatus))]
#endif
        public string trackStatus = TrackNameStatus.FirstOrDefault();

        [Range(0, 1)] public float userFraction = 1;

        public string PackageName       => packageName;
        public string JsonKeyPath       => jsonKeyPath;
        public string ArtifactPath      => artifactPath;
        public string RecentChanges     => recentChangesText;
        public string RecentChangesLang => recentChangedLang;
        public string TrackBranch       => trackBranch;
        public string ReleaseName       => releaseName;
        public float  UserFraction      => userFraction;

        public string TrackStatus => trackStatus;

        public bool IsAppBundle => ArtifactPath != null && ArtifactPath.Contains(AppBundleExtension);

        public IEnumerable<string> GetBranchType()  => BranchVersions;
        public IEnumerable<string> GetTrackStatus() => TrackNameStatus;


        public void Validate()
        {
            packageName       = string.IsNullOrEmpty(packageName) ? PlayerSettings.applicationIdentifier : packageName;
            releaseName       = string.IsNullOrEmpty(releaseName) ? PlayerSettings.bundleVersion : releaseName;
            recentChangesText = string.IsNullOrEmpty(recentChangesText) ? PlayerSettings.bundleVersion : recentChangesText;
        }
    }
}