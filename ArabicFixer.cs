
using System;
using System.Collections.Generic;
using System.Text;

public static class ArabicFixer
{
    private enum ConnectionType
    {
        Transparent,
        Right,
        Dual,
        None
    }

    private class ArabicCharInfo
    {
        public char Isolated { get; set; }
        public char Initial { get; set; }
        public char Medial { get; set; }
        public char Final { get; set; }
        public ConnectionType Connection { get; set; }
    }

    private static readonly Dictionary<char, ArabicCharInfo> _arabicMap = new Dictionary<char, ArabicCharInfo>();

    static ArabicFixer()
    {
        // Populate the Arabic character map
        // The order of these definitions is not important, but completeness is.

        // Hamza (isolated, initial, medial, final, connection type)
        _arabicMap.Add('\u0621', new ArabicCharInfo { Isolated = '\u0621', Initial = '\uFE80', Medial = '\uFE80', Final = '\uFE80', Connection = ConnectionType.None }); // Hamza
        
        // Alef variants
        _arabicMap.Add('\u0623', new ArabicCharInfo { Isolated = '\u0623', Initial = '\uFE83', Medial = '\uFE83', Final = '\uFE84', Connection = ConnectionType.Right }); // Alef with Hamza Above
        _arabicMap.Add('\u0625', new ArabicCharInfo { Isolated = '\u0625', Initial = '\uFE87', Medial = '\uFE87', Final = '\uFE88', Connection = ConnectionType.Right }); // Alef with Hamza Below
        _arabicMap.Add('\u0622', new ArabicCharInfo { Isolated = '\u0622', Initial = '\uFE81', Medial = '\uFE81', Final = '\uFE82', Connection = ConnectionType.Right }); // Alef with Madda Above
        _arabicMap.Add('\u0627', new ArabicCharInfo { Isolated = '\u0627', Initial = '\uFE8D', Medial = '\uFE8D', Final = '\uFE8E', Connection = ConnectionType.Right }); // Alef

        // Baa' family
        _arabicMap.Add('\u0628', new ArabicCharInfo { Isolated = '\uFE8F', Initial = '\uFE91', Medial = '\uFE92', Final = '\uFE90', Connection = ConnectionType.Dual }); // Baa
        _arabicMap.Add('\u062A', new ArabicCharInfo { Isolated = '\uFE95', Initial = '\uFE97', Medial = '\uFE98', Final = '\uFE96', Connection = ConnectionType.Dual }); // Taa
        _arabicMap.Add('\u062B', new ArabicCharInfo { Isolated = '\uFE99', Initial = '\uFE9B', Medial = '\uFE9C', Final = '\uFE9A', Connection = ConnectionType.Dual }); // Thaa
        _arabicMap.Add('\u0629', new ArabicCharInfo { Isolated = '\u0629', Initial = '\uFE93', Medial = '\uFE93', Final = '\uFE94', Connection = ConnectionType.Right }); // Taa Marbuta
        
        // Jeem family
        _arabicMap.Add('\u062C', new ArabicCharInfo { Isolated = '\uFE9D', Initial = '\uFE9F', Medial = '\uFEA0', Final = '\uFE9E', Connection = ConnectionType.Dual }); // Jeem
        _arabicMap.Add('\u062D', new ArabicCharInfo { Isolated = '\uFEA1', Initial = '\uFEA3', Medial = '\uFEA4', Final = '\uFEA2', Connection = ConnectionType.Dual }); // Hah
        _arabicMap.Add('\u062E', new ArabicCharInfo { Isolated = '\uFEA5', Initial = '\uFEA7', Medial = '\uFEA8', Final = '\uFEA6', Connection = ConnectionType.Dual }); // Khah

        // Dal family
        _arabicMap.Add('\u062F', new ArabicCharInfo { Isolated = '\u062F', Initial = '\uFEA9', Medial = '\uFEA9', Final = '\uFEAA', Connection = ConnectionType.Right }); // Dal
        _arabicMap.Add('\u0630', new ArabicCharInfo { Isolated = '\u0630', Initial = '\uFEAB', Medial = '\uFEAB', Final = '\uFEAC', Connection = ConnectionType.Right }); // Thal

        // Ra and Zain
        _arabicMap.Add('\u0631', new ArabicCharInfo { Isolated = '\u0631', Initial = '\uFEAD', Medial = '\uFEAD', Final = '\uFEAE', Connection = ConnectionType.Right }); // Ra
        _arabicMap.Add('\u0632', new ArabicCharInfo { Isolated = '\u0632', Initial = '\uFEAF', Medial = '\uFEAF', Final = '\uFEB0', Connection = ConnectionType.Right }); // Zain

        // Seen and Sheen
        _arabicMap.Add('\u0633', new ArabicCharInfo { Isolated = '\uFEB1', Initial = '\uFEB3', Medial = '\uFEB4', Final = '\uFEB2', Connection = ConnectionType.Dual }); // Seen
        _arabicMap.Add('\u0634', new ArabicCharInfo { Isolated = '\uFEB5', Initial = '\uFEB7', Medial = '\uFEB8', Final = '\uFEB6', Connection = ConnectionType.Dual }); // Sheen

        // Sad and Dad
        _arabicMap.Add('\u0635', new ArabicCharInfo { Isolated = '\uFEB9', Initial = '\uFEBB', Medial = '\uFEBC', Final = '\uFEBA', Connection = ConnectionType.Dual }); // Sad
        _arabicMap.Add('\u0636', new ArabicCharInfo { Isolated = '\uFEBD', Initial = '\uFEBF', Medial = '\uFEC0', Final = '\uFEBE', Connection = ConnectionType.Dual }); // Dad

        // Taa and Dhaa
        _arabicMap.Add('\u0637', new ArabicCharInfo { Isolated = '\uFEC1', Initial = '\uFEC3', Medial = '\uFEC4', Final = '\uFEC2', Connection = ConnectionType.Dual }); // Taa
        _arabicMap.Add('\u0638', new ArabicCharInfo { Isolated = '\uFEC5', Initial = '\uFEC7', Medial = '\uFEC8', Final = '\uFEC6', Connection = ConnectionType.Dual }); // Dhaa

        // Ain and Ghain
        _arabicMap.Add('\u0639', new ArabicCharInfo { Isolated = '\uFEC9', Initial = '\uFECB', Medial = '\uFECC', Final = '\uFECA', Connection = ConnectionType.Dual }); // Ain
        _arabicMap.Add('\u063A', new ArabicCharInfo { Isolated = '\uFECD', Initial = '\uFECF', Medial = '\uFED0', Final = '\uFECE', Connection = ConnectionType.Dual }); // Ghain

        // Faa and Qaf
        _arabicMap.Add('\u0641', new ArabicCharInfo { Isolated = '\uFED1', Initial = '\uFED3', Medial = '\uFED4', Final = '\uFED2', Connection = ConnectionType.Dual }); // Faa
        _arabicMap.Add('\u0642', new ArabicCharInfo { Isolated = '\uFED5', Initial = '\uFED7', Medial = '\uFED8', Final = '\uFED6', Connection = ConnectionType.Dual }); // Qaf

        // Kaf
        _arabicMap.Add('\u0643', new ArabicCharInfo { Isolated = '\uFED9', Initial = '\uFEDB', Medial = '\uFEDC', Final = '\uFEDA', Connection = ConnectionType.Dual }); // Kaf

        // Lam
        _arabicMap.Add('\u0644', new ArabicCharInfo { Isolated = '\uFEDD', Initial = '\uFEDF', Medial = '\uFEE0', Final = '\uFEDE', Connection = ConnectionType.Dual }); // Lam
        _arabicMap.Add('\uFEFB', new ArabicCharInfo { Isolated = '\uFEFB', Initial = '\uFEFB', Medial = '\uFEFC', Final = '\uFEFC', Connection = ConnectionType.Dual }); // Lam-Alef isolated/initial
        _arabicMap.Add('\uFEFC', new ArabicCharInfo { Isolated = '\uFEFB', Initial = '\uFEFB', Medial = '\uFEFC', Final = '\uFEFC', Connection = ConnectionType.Dual }); // Lam-Alef final/medial
        _arabicMap.Add('\uFEF9', new ArabicCharInfo { Isolated = '\uFEF9', Initial = '\uFEF9', Medial = '\uFEFA', Final = '\uFEFA', Connection = ConnectionType.Dual }); // Lam-Alef with Hamza Below
        _arabicMap.Add('\uFEFA', new ArabicCharInfo { Isolated = '\uFEF9', Initial = '\uFEF9', Medial = '\uFEFA', Final = '\uFEFA', Connection = ConnectionType.Dual }); // Lam-Alef with Hamza Below
        _arabicMap.Add('\uFEF7', new ArabicCharInfo { Isolated = '\uFEF7', Initial = '\uFEF7', Medial = '\uFEF8', Final = '\uFEF8', Connection = ConnectionType.Dual }); // Lam-Alef with Hamza Above
        _arabicMap.Add('\uFEF8', new ArabicCharInfo { Isolated = '\uFEF7', Initial = '\uFEF7', Medial = '\uFEF8', Final = '\uFEF8', Connection = ConnectionType.Dual }); // Lam-Alef with Hamza Above
        _arabicMap.Add('\uFEF5', new ArabicCharInfo { Isolated = '\uFEF5', Initial = '\uFEF5', Medial = '\uFEF6', Final = '\uFEF6', Connection = ConnectionType.Dual }); // Lam-Alef with Madda Above
        _arabicMap.Add('\uFEF6', new ArabicCharInfo { Isolated = '\uFEF5', Initial = '\uFEF5', Medial = '\uFEF6', Final = '\uFEF6', Connection = ConnectionType.Dual }); // Lam-Alef with Madda Above

        // Meem
        _arabicMap.Add('\u0645', new ArabicCharInfo { Isolated = '\uFEE1', Initial = '\uFEE3', Medial = '\uFEE4', Final = '\uFEE2', Connection = ConnectionType.Dual }); // Meem

        // Noon
        _arabicMap.Add('\u0646', new ArabicCharInfo { Isolated = '\uFEE5', Initial = '\uFEE7', Medial = '\uFEE8', Final = '\uFEE6', Connection = ConnectionType.Dual }); // Noon

        // Haa
        _arabicMap.Add('\u0647', new ArabicCharInfo { Isolated = '\uFEE9', Initial = '\uFEEB', Medial = '\uFEEC', Final = '\uFEEA', Connection = ConnectionType.Dual }); // Haa

        // Waw and Waw with Hamza Above
        _arabicMap.Add('\u0648', new ArabicCharInfo { Isolated = '\u0648', Initial = '\uFEED', Medial = '\uFEED', Final = '\uFEEE', Connection = ConnectionType.Right }); // Waw
        _arabicMap.Add('\u0624', new ArabicCharInfo { Isolated = '\u0624', Initial = '\uFE85', Medial = '\uFE85', Final = '\uFE86', Connection = ConnectionType.Right }); // Waw with Hamza Above

        // Yaa and Yaa with Hamza Above
        _arabicMap.Add('\u064A', new ArabicCharInfo { Isolated = '\uFEF1', Initial = '\uFEF3', Medial = '\uFEF4', Final = '\uFEF2', Connection = ConnectionType.Dual }); // Yaa
        _arabicMap.Add('\u0626', new ArabicCharInfo { Isolated = '\uFE89', Initial = '\uFE8B', Medial = '\uFE8C', Final = '\uFE8A', Connection = ConnectionType.Dual }); // Yaa with Hamza Above
        _arabicMap.Add('\u0649', new ArabicCharInfo { Isolated = '\u0649', Initial = '\uFEEF', Medial = '\uFEEF', Final = '\uFEF0', Connection = ConnectionType.Right }); // Alef Maksura (Yaa without dots)

        // Other common characters
        _arabicMap.Add('\u067E', new ArabicCharInfo { Isolated = '\u067E', Initial = '\uFB56', Medial = '\uFB58', Final = '\uFB57', Connection = ConnectionType.Dual }); // Peh
        _arabicMap.Add('\u0686', new ArabicCharInfo { Isolated = '\u0686', Initial = '\uFB7A', Medial = '\uFB7C', Final = '\uFB7B', Connection = ConnectionType.Dual }); // Cheh
        _arabicMap.Add('\u0698', new ArabicCharInfo { Isolated = '\u0698', Initial = '\uFB8A', Medial = '\uFB8A', Final = '\uFB8B', Connection = ConnectionType.Right }); // Jeh
        _arabicMap.Add('\u06AF', new ArabicCharInfo { Isolated = '\u06AF', Initial = '\uFB92', Medial = '\uFB94', Final = '\uFB93', Connection = ConnectionType.Dual }); // Gaf
        _arabicMap.Add('\u06A4', new ArabicCharInfo { Isolated = '\u06A4', Initial = '\uFB62', Medial = '\uFB64', Final = '\uFB63', Connection = ConnectionType.Dual }); // Veh (Keh with three dots)

        // Tatweel (Kashida)
        _arabicMap.Add('\u0640', new ArabicCharInfo { Isolated = '\u0640', Initial = '\u0640', Medial = '\u0640', Final = '\u0640', Connection = ConnectionType.Dual }); // Tatweel/Kashida

        // Diacritics (treated as transparent, do not affect connection)
        _arabicMap.Add('\u064B', new ArabicCharInfo { Isolated = '\u064B', Initial = '\u064B', Medial = '\u064B', Final = '\u064B', Connection = ConnectionType.Transparent }); // Fathatan
        _arabicMap.Add('\u064C', new ArabicCharInfo { Isolated = '\u064C', Initial = '\u064C', Medial = '\u064C', Final = '\u064C', Connection = ConnectionType.Transparent }); // Dammatan
        _arabicMap.Add('\u064D', new ArabicCharInfo { Isolated = '\u064D', Initial = '\u064D', Medial = '\u064D', Final = '\u064D', Connection = ConnectionType.Transparent }); // Kasratan
        _arabicMap.Add('\u064E', new ArabicCharInfo { Isolated = '\u064E', Initial = '\u064E', Medial = '\u064E', Final = '\u064E', Connection = ConnectionType.Transparent }); // Fatha
        _arabicMap.Add('\u064F', new ArabicCharInfo { Isolated = '\u064F', Initial = '\u064F', Medial = '\u064F', Final = '\u064F', Connection = ConnectionType.Transparent }); // Damma
        _arabicMap.Add('\u0650', new ArabicCharInfo { Isolated = '\u0650', Initial = '\u0650', Medial = '\u0650', Final = '\u0650', Connection = ConnectionType.Transparent }); // Kasra
        _arabicMap.Add('\u0651', new ArabicCharInfo { Isolated = '\u0651', Initial = '\u0651', Medial = '\u0651', Final = '\u0651', Connection = ConnectionType.Transparent }); // Shadda
        _arabicMap.Add('\u0652', new ArabicCharInfo { Isolated = '\u0652', Initial = '\u0652', Medial = '\u0652', Final = '\u0652', Connection = ConnectionType.Transparent }); // Sukun
        _arabicMap.Add('\u0670', new ArabicCharInfo { Isolated = '\u0670', Initial = '\u0670', Medial = '\u0670', Final = '\u0670', Connection = ConnectionType.Transparent }); // Dagger Alef

        // Other characters (not typically shaped)
        _arabicMap.Add('\u060C', new ArabicCharInfo { Isolated = '\u060C', Initial = '\u060C', Medial = '\u060C', Final = '\u060C', Connection = ConnectionType.None }); // Arabic Comma
        _arabicMap.Add('\u061B', new ArabicCharInfo { Isolated = '\u061B', Initial = '\u061B', Medial = '\u061B', Final = '\u061B', Connection = ConnectionType.None }); // Arabic Semicolon
        _arabicMap.Add('\u061F', new ArabicCharInfo { Isolated = '\u061F', Initial = '\u061F', Medial = '\u061F', Final = '\u061F', Connection = ConnectionType.None }); // Arabic Question Mark
        // Add other non-shaping characters as needed with ConnectionType.None or Transparent
    }

    // Helper to get character info, returns null if not an Arabic character we handle
    private static ArabicCharInfo GetCharInfo(char c)
    {
        _arabicMap.TryGetValue(c, out var info);
        return info;
    }

    /// <summary>
    /// Reshapes Arabic text to display correctly with contextual forms and RTL order.
    /// </summary>
    /// <param name="text">The original Arabic text (logical order).</param>
    /// <param name="removeTashkeel">If true, diacritics (tashkeel) will be removed from the output.</param>
    /// <returns>The reshaped and RTL-ordered text.</returns>
    public static string Reshape(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        StringBuilder normalizedTextBuilder = new StringBuilder(text.Length);
        for (int i = 0; i < text.Length; i++)
        {
            char c = text[i];
            if (c == '\u200B' || c == '\u200C' || c == '\u200D' || c == '\u200E' || c == '\u200F' ||
                c == '\u061C' || c == '\u2060' || c == '\u2066' || c == '\u2067' || c == '\u2068' ||
                c == '\u2069' || c == '\u202A' || c == '\u202B' || c == '\u202C' || c == '\u202D' ||
                c == '\u202E' || c == '\uFEFF')
            {
                continue;
            }

            normalizedTextBuilder.Append(c);
        }

        string normalizedText = normalizedTextBuilder.ToString();
        StringBuilder shapedTextBuilder = new StringBuilder();

        for (int i = 0; i < normalizedText.Length; i++)
        {
            char current = normalizedText[i];
            ArabicCharInfo currentInfo = GetCharInfo(current);

            if (currentInfo == null)
            {
                shapedTextBuilder.Append(current);
                continue;
            }

            ArabicCharInfo prevInfo = null;
            for (int prevIndex = i - 1; prevIndex >= 0; prevIndex--)
            {
                prevInfo = GetCharInfo(normalizedText[prevIndex]);
                if (prevInfo == null || prevInfo.Connection != ConnectionType.Transparent)
                {
                    break;
                }
            }

            ArabicCharInfo nextInfo = null;
            for (int nextIndex = i + 1; nextIndex < normalizedText.Length; nextIndex++)
            {
                nextInfo = GetCharInfo(normalizedText[nextIndex]);
                if (nextInfo == null || nextInfo.Connection != ConnectionType.Transparent)
                {
                    break;
                }
            }

            bool connectsToRight = prevInfo != null && (prevInfo.Connection == ConnectionType.Dual || prevInfo.Connection == ConnectionType.Right);
            bool connectsToLeft = nextInfo != null && (nextInfo.Connection == ConnectionType.Dual || nextInfo.Connection == ConnectionType.Right);

            if (currentInfo.Connection == ConnectionType.Dual)
            {
                if (connectsToRight && connectsToLeft)
                    shapedTextBuilder.Append(currentInfo.Medial);
                else if (connectsToRight)
                    shapedTextBuilder.Append(currentInfo.Final);
                else if (connectsToLeft)
                    shapedTextBuilder.Append(currentInfo.Initial);
                else
                    shapedTextBuilder.Append(currentInfo.Isolated);
            }
            else if (currentInfo.Connection == ConnectionType.Right)
            {
                if (connectsToRight)
                    shapedTextBuilder.Append(currentInfo.Final);
                else
                    shapedTextBuilder.Append(currentInfo.Isolated);
            }
            else
            {
                shapedTextBuilder.Append(currentInfo.Isolated);
            }
        }

        return shapedTextBuilder.ToString();
    }

    /// <summary>
    /// Checks if a character is an Arabic character or a diacritic.
    /// </summary>
    public static bool IsArabicChar(char c)
    {
        return GetCharInfo(c) != null;
    }

    public static bool IsArabicPresentationForm(char c)
    {
        return (c >= '\uFB50' && c <= '\uFDFF') || (c >= '\uFE70' && c <= '\uFEFF');
    }

    public static string GetPresentationFormsCharset()
    {
        HashSet<char> chars = new HashSet<char>();
        foreach (var entry in _arabicMap)
        {
            chars.Add(entry.Value.Isolated);
            chars.Add(entry.Value.Initial);
            chars.Add(entry.Value.Medial);
            chars.Add(entry.Value.Final);
        }

        chars.Add(' ');
        chars.Add(':');
        chars.Add('.');
        chars.Add('؟');
        chars.Add('،');
        chars.Add('؛');

        StringBuilder sb = new StringBuilder(chars.Count);
        foreach (char c in chars)
        {
            sb.Append(c);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Removes all diacritics (tashkeel) from an Arabic string.
    /// </summary>
    public static string RemoveTashkeel(string text)
    {
        if (string.IsNullOrEmpty(text))
            return text;

        StringBuilder sb = new StringBuilder();
        foreach (char c in text)
        {
            ArabicCharInfo charInfo = GetCharInfo(c);
            if (charInfo == null || charInfo.Connection != ConnectionType.Transparent)
            {
                sb.Append(c);
            }
        }
        return sb.ToString();
    }
}
