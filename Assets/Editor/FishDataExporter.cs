// FishDataExporter.cs
// Exports all fish ItemDefinitions in Assets/Resources/Fishes to ExportedData/fishes.json
// (project root, next to Assets). The file is committed to git and consumed by the
// server database backend. Runs automatically before every player build and can be
// triggered manually via Tools > Export Fish Data.
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using ItemSystem;
using Newtonsoft.Json;
using UnityEditor;
using UnityEditor.Build;
using UnityEditor.Build.Reporting;
using UnityEngine;
using Utils;

public class FishDataExporter : IPreprocessBuildWithReport
{
    const string FishesAssetFolder = "Assets/Resources/Fishes";
    const string OutputRelativePath = "ExportedData/fishes.json";
    // Bump when the JSON structure changes, so the backend can detect incompatible files.
    const int FormatVersion = 1;

    public int callbackOrder => 0;

    public void OnPreprocessBuild(BuildReport report)
    {
        try
        {
            Export();
        }
        catch (Exception e)
        {
            // Ensure a stale or broken fishes.json never ships silently with a build.
            throw new BuildFailedException($"Fish data export failed: {e.Message}");
        }
    }

    [MenuItem("Tools/Export Fish Data")]
    public static void ExportMenuItem()
    {
        Export();
    }

    static void Export()
    {
        string[] guids = AssetDatabase.FindAssets("t:ItemDefinition", new[] { FishesAssetFolder });
        if (guids.Length == 0)
        {
            throw new InvalidOperationException($"No ItemDefinition assets found in {FishesAssetFolder}");
        }

        var errors = new List<string>();
        var fishes = new List<FishDto>();

        foreach (string guid in guids)
        {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            var definition = AssetDatabase.LoadAssetAtPath<ItemDefinition>(assetPath);
            FishBehaviour behaviour = definition.GetBehaviour<FishBehaviour>();
            if (behaviour == null)
            {
                errors.Add($"{assetPath} has no FishBehaviour");
                continue;
            }

            fishes.Add(new FishDto
            {
                id = definition.Id,
                display_name = definition.DisplayName,
                max_stack = definition.MaxStack,
                is_static = definition.IsStatic,
                infinite_use = definition.InfiniteUse,
                minimum_length = behaviour.MinimumLength,
                maximum_length = behaviour.MaximumLength,
                average_length = behaviour.AvarageLength,
                min_market_price = behaviour.MinMartketPrice,
                max_market_price = behaviour.MaxMarketPrice,
                bites_on = FlagNames(behaviour.BitesOn),
                rarity = behaviour.Rarity.ToString(),
                locations = FlagNames(behaviour.Locations),
                rarity_factor = behaviour.RarityFactor,
                time_ranges = behaviour.TimeRanges.Select(t => new TimeRangeDto
                {
                    begin = new TimeDto { hours = t.BeginTime.hours, minutes = t.BeginTime.minutes },
                    end = new TimeDto { hours = t.EndTime.hours, minutes = t.EndTime.minutes },
                }).ToList(),
                date_ranges = behaviour.DateRanges.Select(d => new DateRangeDto
                {
                    begin = new DateDto { month = d.BeginDate.month, day = d.BeginDate.day },
                    end = new DateDto { month = d.EndDate.month, day = d.EndDate.day },
                }).ToList(),
            });
        }

        var duplicateIds = fishes.GroupBy(f => f.id).Where(g => g.Count() > 1).Select(g => g.Key).ToList();
        if (duplicateIds.Count > 0)
        {
            errors.Add($"Duplicate fish ids: {string.Join(", ", duplicateIds)}");
        }

        if (errors.Count > 0)
        {
            throw new InvalidOperationException(string.Join("; ", errors));
        }

        // Deterministic order so the committed file only changes when fish data changes.
        fishes.Sort((a, b) => a.id.CompareTo(b.id));

        var export = new FishExportDto
        {
            format_version = FormatVersion,
            fishes = fishes,
        };

        string projectRoot = Directory.GetParent(Application.dataPath).FullName;
        string outputPath = Path.Combine(projectRoot, OutputRelativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(outputPath));
        File.WriteAllText(outputPath, JsonConvert.SerializeObject(export, Formatting.Indented) + "\n");

        Debug.Log($"Exported {fishes.Count} fishes to {outputPath}");
    }

    // Decomposes a [Flags] enum value into the names of its set flags.
    static List<string> FlagNames<T>(T value) where T : Enum
    {
        int bits = Convert.ToInt32(value);
        return Enum.GetValues(typeof(T))
            .Cast<T>()
            .Where(flag => (bits & Convert.ToInt32(flag)) != 0)
            .Select(flag => flag.ToString())
            .ToList();
    }

    [Serializable]
    class FishExportDto
    {
        public int format_version;
        public List<FishDto> fishes;
    }

    [Serializable]
    class FishDto
    {
        public int id;
        public string display_name;
        public int max_stack;
        public bool is_static;
        public bool infinite_use;
        public int minimum_length;
        public int maximum_length;
        public int average_length;
        public int min_market_price;
        public int max_market_price;
        public List<string> bites_on;
        public string rarity;
        public List<string> locations;
        public float rarity_factor;
        public List<TimeRangeDto> time_ranges;
        public List<DateRangeDto> date_ranges;
    }

    [Serializable]
    class TimeRangeDto
    {
        public TimeDto begin;
        public TimeDto end;
    }

    [Serializable]
    class TimeDto
    {
        public int hours;
        public int minutes;
    }

    [Serializable]
    class DateRangeDto
    {
        public DateDto begin;
        public DateDto end;
    }

    [Serializable]
    class DateDto
    {
        public int month;
        public int day;
    }
}
