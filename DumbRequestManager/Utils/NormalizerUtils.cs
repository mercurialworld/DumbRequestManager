namespace DumbRequestManager.Utils;

internal abstract class Normalize
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
                return "#90DegreesIcon";
            case "ThreeSixtyDegree":
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
        switch (characteristic)
        {
            case "SingleSaber":
            case "OneSaber":
                return "OneSaber";
            case "NinetyDegree":
            case "90Degree":
                return "90Degree";
            case "ThreeSixtyDegree":
            case "360Degree":
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
}

