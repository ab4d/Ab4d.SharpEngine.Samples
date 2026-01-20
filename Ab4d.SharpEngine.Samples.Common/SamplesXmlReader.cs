using System.Collections.Generic;

namespace Ab4d.SharpEngine.Samples.Common;

public class SamplesXmlReader
{
    public class SampleItem
    {
        public string Location { get; }
        public string Title { get; }
        public bool IsTitle { get; }
        public bool IsSeparator { get; set; }
        public bool IsNew { get; set; } 
        public bool IsUpdated { get; set; }
        public string UpdateInfo { get; set; }
        public string? Condition { get; set; }

        public SampleItem(string? location, string? title, bool isTitle = false, bool isSeparator = false, bool isNew = false, string? updateInfo = null, string? condition = null)
        {
            Location = location ?? "";
            Title = title ?? "";
            IsTitle = isTitle;
            IsSeparator = isSeparator;
            IsNew = isNew;
            IsUpdated = updateInfo != null;
            UpdateInfo = updateInfo ?? "";
            Condition = condition;
        }
    }

    public class SampleGroup
    {
        public string Location { get; set; }
        public string Title { get; set; }
        public List<SampleItem> Items { get; set; } = new();

        public SampleGroup()
        {
            Location = "";
            Title = "";
        }

        public SampleGroup(string? location, string? title)
        {
            Location = location ?? "";
            Title = title ?? "";
        }
    }

    //public static List<SampleGroup> GetTestSamples(string xmlFilePath)
    //{
    //    var samples = new List<SampleItem>()
    //    {
    //        new SampleItem("Titles.CamerasTitle.md", "Cameras", isTitle: true),
    //        new SampleItem("Cameras.TargetPositionCameraSample", "TargetPositionCamera"),
    //        new SampleItem("Cameras.FreeCameraSample", "FreeCamera", isNew: true),
    //        new SampleItem("", "", isSeparator: true),
    //        new SampleItem("Cameras.FirstPersonCameraSample", "FirstPersonCamera", updateInfo: "TEST UPDATE INFO"),

    //        new SampleItem("Titles.PointerCameraControllerTitle.md", "PointerCameraController", isTitle: true),
    //        new SampleItem("CameraControllers.PointerCameraControllerSample", "PointerCameraController"),
    //    };

    //    var groups = BuildGroups(samples);
    //    return groups;
    //}

    public static List<SampleGroup> BuildGroups(List<SampleItem> items)
    {
        var groups = new List<SampleGroup>();
        SampleGroup? current = null;
        bool addSeparatorBeforeNext = false;

        foreach (var item in items)
        {
            if (item.IsTitle)
            {
                current = new SampleGroup
                {
                    Title = item.Title ?? "",
                    Location = item.Location ?? ""
                };
                groups.Add(current);

                addSeparatorBeforeNext = false;
            }
            else if (item.IsSeparator)
            {
                addSeparatorBeforeNext = true;
            }
            else if (current != null)
            {
                if (addSeparatorBeforeNext) 
                { 
                    // Mark this item as needing a separator before it
                    item.IsSeparator = true; 
                    addSeparatorBeforeNext = false;
                }

                current.Items.Add(item);
            }
        }

        return groups;
    }

    // Because we have a very simple xml structure it is much faster to have a simple custom xml parser
    // then to relay on .Net's XML libraries that need to support all the complexities of the xml format.
    public static List<SampleItem> ParseXml(string samplesXmlContent)
    {
        var result = new List<SampleItem>();

        var xmlLines = samplesXmlContent.Split("\n");

        bool isComment = false;

        foreach (var rawLine in xmlLines)
        {
            var line = rawLine.Trim();

            if (line.Length == 0)
                continue;

            if (isComment)
            {
                if (line.EndsWith("-->")) 
                    isComment = false;

                continue;
            }

            if (line.StartsWith("<!--"))
            {
                if (!line.EndsWith("-->"))
                    isComment = true;

                continue;
            }

            // Only parse <Sample ... />
            if (!line.StartsWith("<Sample ")) // skip "<Samples"
                continue;

            // Extract attributes
            var item = new SampleItem(
                location:    GetAttribute(line, "Location"),
                title:       GetAttribute(line, "Title"),
                isTitle:     GetBoolAttribute(line, "IsTitle"),
                isSeparator: GetBoolAttribute(line, "IsSeparator"),
                isNew:       GetBoolAttribute(line, "IsNew"),
                updateInfo:  GetAttribute(line, "UpdateInfo"),
                condition:   GetAttribute(line, "Condition"));

            result.Add(item);
        }

        return result;
    }

    private static string? GetAttribute(string line, string name)
    {
        var key = " " + name + "=\""; // we need to add space before name to avoid partial matches (for example "IsTitle" matching "Title")
        var start = line.IndexOf(key, StringComparison.Ordinal);
        if (start < 0)
            return null;

        start += key.Length;
        var end = line.IndexOf('"', start);
        if (end < 0)
            return null;

        var attributeText = line.Substring(start, end - start);
        attributeText = attributeText.Replace("&amp;", "&").Replace("&quot;", "\"");
        return attributeText;
    }

    private static bool GetBoolAttribute(string line, string name)
    {
        var value = GetAttribute(line, name);
        return value != null && value.Equals("true", StringComparison.OrdinalIgnoreCase);
    }

}