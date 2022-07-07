using System;
using System.Collections.Generic;
using System.Text;
using System.Text.RegularExpressions;

namespace MarlinEasyConfig
{
    public static class StringExtensions
    {
        public static string ReplaceFirst(this string text, string search, string replace)
        {
            int pos = text.IndexOf(search);
            if (pos < 0) return text;
            return text.Substring(0, pos) + replace + text.Substring(pos + search.Length);
        }
        public static string ReplaceAll(this string seed, string[] chars, string replacementCharacter)
        {
            foreach (string c in chars)
            {
                seed = seed.Replace(c, replacementCharacter);
            }
            return seed;
        }
        public static string EscapeQuotes(this string str)
        {
            return Regex.Replace(str, @"\\([\s\S])|([\""|\'])", "\\$1$2");
        }
        public static string EscapeInnerQuotes(this string str)
        {
            return Regex.Replace(str, @"\\([\s\S])|([\""|\'])", "\\$1$2");
        }
    }
}
