using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;

namespace DumbRequestManager.Utils;

internal static class Extensions
{
    // https://stackoverflow.com/a/7471047
    public static string RemoveDiacritics(this string s)
    {
        string normalizedString = s.Normalize(NormalizationForm.FormD);
        StringBuilder stringBuilder = new();

        foreach (char c in normalizedString.Where(c => CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark))
        {
            stringBuilder.Append(c);
        }

        return stringBuilder.ToString();
    }
}
internal abstract class Censor
{
    private static readonly string CensorWordsFilename = Path.Combine(Plugin.UserDataDir, "CensorWords.txt");
    private static readonly Dictionary<string, bool> CensorWords = new();

    private static readonly string[] AgePhrases =
    [
        "# year old",
        "# year young",
        "im #",
        "i am #",
        "age of #",
        "the age of #"
    ];

    private static void InitializeCensorWords()
    {
        if (!File.Exists(CensorWordsFilename))
        {
            return;
        }
        
        string[] lines = File.ReadAllLines(CensorWordsFilename);

        foreach (string line in lines)
        {
            if (string.IsNullOrEmpty(line))
            {
                continue;
            }
            if (line[..2] == "//")
            {
                continue;
            }
            
            string[] parts = line.ToLowerInvariant().RemoveDiacritics().Split(' ');
            if (parts.Length == 1)
            {
                CensorWords.Add(parts[0], true);
            }
            else
            {
                CensorWords.Add(parts[1], parts[0] == "a");
            }
        }
        
        Plugin.Log.Info($"CensorWords contains {CensorWords.Count} words");
    }

    private static bool StringContainsCensoredWord(string input)
    {
        if (!File.Exists(CensorWordsFilename))
        {
            return false;
        }
        
        if (CensorWords.Count == 0)
        {
            InitializeCensorWords();
        }
        
        string strippedInput = input.ToLowerInvariant().RemoveDiacritics();
            
        string aggressiveStrippedInput = input.Replace(" ", string.Empty);
        string[] softStrippedInput = strippedInput.Split(' ');
        
        foreach ((string word, bool aggressive) in CensorWords)
        {
            if (aggressive ? aggressiveStrippedInput.Contains(word) : softStrippedInput.Contains(word))
            {
                return true;
            }
        }
        
        return false;
    }

    private static bool StringContainsAgePhrase(string input)
    {
        Regex step1 = new (@"[^a-rt-zA-RT-Z0-9 \s]");
        Regex step2 = new ("[0-9]");
        string normalized = step1.Replace(input.ToLowerInvariant().RemoveDiacritics(), string.Empty);
        normalized = step2.Replace(normalized, "#");
        normalized = Regex.Replace(normalized, "#+", "#");

        return AgePhrases.Any(agePhrase => normalized.Contains(agePhrase));
    }

    private static bool SongContainsSplicedPhrase(string[] parts)
    {
        if (!File.Exists(CensorWordsFilename))
        {
            return false;
        }
        
        if (CensorWords.Count == 0)
        {
            InitializeCensorWords();
        }
        
        Regex onlyAlphanumeric = new (@"[^a-zA-Z0-9 \s]");

        List<string> inputs = [];
        // ReSharper disable once LoopCanBeConvertedToQuery
        for (int i = 0; i < parts.Length; i++)
        {
            inputs.Add(onlyAlphanumeric.Replace(
                $"{parts[i % parts.Length]}{parts[(i + 1) % parts.Length]}{parts[(i + 2) % parts.Length]}{parts[(i + 3) % parts.Length]}"
                    .ToLowerInvariant().RemoveDiacritics().Replace(" ", string.Empty), string.Empty));
        }

        foreach (string input in inputs)
        {
            Plugin.DebugMessage($"Checking for stuff in {input}");
            foreach ((string word, bool aggressive) in CensorWords)
            {
                if (aggressive && input.Contains(word))
                {
                    return true;
                }
            }   
        }
        
        return false;
    }

    public static bool Check(string input)
    {
        return StringContainsCensoredWord(input) || StringContainsAgePhrase(input);
    }
    public static bool Check(string[] parts)
    {
        return SongContainsSplicedPhrase(parts);
    }
}