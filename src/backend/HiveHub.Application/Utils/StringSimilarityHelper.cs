namespace HiveHub.Application.Utils;

public static class StringSimilarityHelper
{
    public static bool IsCloseEnough(string input, string target)
    {
        if (string.IsNullOrWhiteSpace(input) || string.IsNullOrWhiteSpace(target))
            return false;

        // Normalize: remove spaces and lowercase
        var s1 = input.Trim().ToLowerInvariant();
        var s2 = target.Trim().ToLowerInvariant();

        if (s1 == s2) return true;

        var distance = ComputeLevenshteinDistance(s1, s2);
        var length = Math.Max(s1.Length, s2.Length);

        // Dynamic threshold logic:
        // Length <= 3: Must be exact match (0 tolerance)
        // Length 4-8: Allow 1 mistake (typo, missing letter)
        // Length > 8: Allow 2 mistakes
        int maxAllowedDistance = length switch
        {
            <= 3 => 0,
            <= 8 => 1,
            _ => 2
        };

        return distance <= maxAllowedDistance;
    }

    /// <summary>
    /// Classic Dynamic Programming implementation of Levenshtein Distance
    /// Time Complexity: O(N*M), Space Complexity: O(N*M)
    /// </summary>
    private static int ComputeLevenshteinDistance(string s, string t)
    {
        int n = s.Length;
        int m = t.Length;
        int[,] d = new int[n + 1, m + 1];

        if (n == 0) return m;
        if (m == 0) return n;

        for (int i = 0; i <= n; d[i, 0] = i++) { }
        for (int j = 0; j <= m; d[0, j] = j++) { }

        for (int i = 1; i <= n; i++)
        {
            for (int j = 1; j <= m; j++)
            {
                int cost = (t[j - 1] == s[i - 1]) ? 0 : 1;

                d[i, j] = Math.Min(
                    Math.Min(d[i - 1, j] + 1,      // Deletion
                             d[i, j - 1] + 1),     // Insertion
                    d[i - 1, j - 1] + cost);       // Substitution
            }
        }

        return d[n, m];
    }
}