namespace ForgeLauncher.WPF.Extensions;

public static class VersionComparer
{
    //https://www.geeksforgeeks.org/compare-two-version-numbers/amp/
    public static int Compare(string v1, string v2)
    {
        // vnum stores each numeric
        // part of version
        int vnum1 = 0, vnum2 = 0;
        // loop until both string are
        // processed
        for (int i = 0, j = 0; (i < v1.Length || j < v2.Length);)
        {
            // storing numeric part of
            // version 1 in vnum1
            while (i < v1.Length && v1[i] != '.')
            {
                vnum1 = vnum1 * 10 + (v1[i] - '0');
                i++;
            }
            // storing numeric part of
            // version 2 in vnum2
            while (j < v2.Length && v2[j] != '.')
            {
                vnum2 = vnum2 * 10 + (v2[j] - '0');
                j++;
            }
            if (vnum1 > vnum2)
                return 1;

            if (vnum2 > vnum1)
                return -1;

            // if equal, reset variables and
            // go for next numeric part
            vnum1 = vnum2 = 0;
            i++;
            j++;
        }
        return 0;
    }
}
