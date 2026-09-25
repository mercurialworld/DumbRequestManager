using System.IO;
using System.Linq;

namespace DumbRequestManager.Utils;

internal static class Normalize
{
    public static string GetCharacteristicIcon(string characteristic)
    {
        return characteristic switch
        {
            "OneSaber" => "#SingleSaberIcon",
            "NoArrows" => "#NoArrowsIcon",
            "NinetyDegree" or "90Degree" => "#90DegreesIcon",
            "ThreeSixtyDegree" or "360Degree" => "#360DegreesIcon",
            "Custom" or "Legacy" => // wtf
                "#LegacyIcon",
            "Lightshow" =>
                // coming back to this later maybe
                // return SongCore.Loader.beatmapCharacteristicCollection.beatmapCharacteristics[0].icon;
                "#LightIcon",
            "Lawless" => "#FailIcon",
            _ => $"#{characteristic}BeatmapCharacteristicIcon"
        };
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

        return characteristic switch
        {
            "SingleSaber" or "OneSaber" => "OneSaber",
            "NinetyDegree" or "90Degree" or "_90Degree" => "90Degree",
            "ThreeSixtyDegree" or "360Degree" or "_360Degree" => "360Degree",
            _ => characteristic
        };
    }

    public static string GetDifficultyName(string difficulty)
    {
        return difficulty switch
        {
            "E" or "Easy" => "Easy",
            "N" or "Normal" => "Normal",
            "H" or "Hard" => "Hard",
            "X" or "Expert" => "Expert",
            _ => "Expert+"
        };
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

