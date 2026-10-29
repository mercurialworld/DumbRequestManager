namespace DumbRequestManager.Utils;

internal static class StringExtensions
{
    // https://gist.github.com/Caeden117/1e0187d8f7f89717974a1b558c0acc2a
    
    /*
     * Original code by @lolPants, put into an Extension class.
     *
     * The second replace is not necessary, but is there for completeness. If you need to strip TMP tags regularly,
     * you can easily remove it for added performance.
     */
    public static string StripTMPTags(this string source) => source.Replace(@"<", "<\u200B").Replace(@">", "\u200B>");
}