using System.IO;
using System.Linq;

namespace DumbRequestManager.Utils;

internal static class Normalize
{
    public static string GetCharacteristicIcon(string characteristic)
    {
        switch (characteristic)
        {
            case "OneSaber":
                return "#SingleSaberIcon";
            case "NoArrows":
                return "#NoArrowsIcon";
            case "NinetyDegree":
            case "90Degree":
                return "#90DegreesIcon";
            case "ThreeSixtyDegree":
            case "360Degree":
                return "#360DegreesIcon";
            case "Custom" or "Legacy": // wtf
                return "#LegacyIcon";
            case "Lightshow":
                // coming back to this later maybe
                // return SongCore.Loader.beatmapCharacteristicCollection.beatmapCharacteristics[0].icon;
                return "#LightIcon";
            case "Lawless":
                return "#FailIcon";
            
            default:
                return $"#{characteristic}BeatmapCharacteristicIcon";
        }
    }

    public static string GetCharacteristicName(string characteristic)
    {
        // for a 90 Degree map:
        
        // BeatSaverSharp
        // new BeatSaver().Beatmap().Result.LatestVersion.Difficulties[0].Characteristic -> _90Degree
        
        // SongDetailsCache
        // new Song().difficulties.First().characteristic. -> NinetyDegree
        
        // Base game
        // new BeatmapKey().beatmapCharacteristic.serializedName -> 90Degree
        
        // ...can we like, come together and figure this out? Can we agree.
        
        switch (characteristic)
        {
            case "SingleSaber":
            case "OneSaber":
                return "OneSaber";
            case "NinetyDegree":
            case "90Degree":
            case "_90Degree":
                return "90Degree";
            case "ThreeSixtyDegree":
            case "360Degree":
            case "_360Degree":
                return "360Degree";
            
            default:
                return characteristic;
        }
    }

    public static string GetDifficultyName(string difficulty)
    {
        switch (difficulty)
        {
            case "E" or "Easy":
                return "Easy";
            case "N" or "Normal":
                return "Normal";
            case "H" or "Hard":
                return "Hard";
            case "X" or "Expert":
                return "Expert";
            
            default:
                return "Expert+";
        }
    }

    // https://stackoverflow.com/a/62596727
    public static string StripSomeControlCharacters(this string text)
    {
        char[] chars = new char[text.Length];
        int outputIndex = 0;

        foreach (char character in text.Where(character =>
                         !Path.GetInvalidPathChars().Contains(character) &&
                         !Path.GetInvalidFileNameChars().Contains(character)))
        {
            chars[outputIndex++] = character;
        }
        
        return new string(chars, 0, outputIndex);
    }
}

