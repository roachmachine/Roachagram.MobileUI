using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;

namespace Roachagram.MobileUI.Helpers
{
    public static class TextFormatHelper
    {
        public static string DecodeApiString(string raw)
        {
            // Step 1: Decode Unicode escape sequences
            string unicodeDecoded = Regex.Unescape(raw);

            // Step 2: Decode HTML entities
            string htmlDecoded = WebUtility.HtmlDecode(unicodeDecoded);

            // Step 3: Replace \n with <br> or wrap in <p> tags
            string formatted = htmlDecoded.Replace("\n", "<br>");
        
            return formatted;
        }

        public static string ReplaceMarkdownBoldWithHtmlBold(string input)
        {
            // Replace **example** with <b>Example</b> (capitalize each word in bold)
            return Regex.Replace(input, @"\*\*(.+?)\*\*", match =>
            {
                string content = match.Groups[1].Value;
                string capitalizedContent = string.Join(" ", content.Split(' ')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
                return $"<b>{capitalizedContent}</b>";
            });




        }

        public static string CapitalizeWordsInQuotes(string input)
        {
            // Match content within double quotes and capitalize each word
            return Regex.Replace(input, "\"(.*?)\"", match =>
            {
                string content = match.Groups[1].Value;
                string capitalizedContent = string.Join(" ", content.Split(' ')
                    .Select(word => char.ToUpper(word[0]) + word.Substring(1).ToLower()));
                return $"\"{capitalizedContent}\"";
            });


        }

        public static string BoldSectionAfterHashes(string input)
        {
            // Replace lines starting with ### and a phrase (ending with a colon) with bolded phrase
            return Regex.Replace(input, @"^###\s*([^\r\n:]+:)", m => $"<b>{m.Groups[1].Value}</b>", RegexOptions.Multiline);
        }
    }
}
