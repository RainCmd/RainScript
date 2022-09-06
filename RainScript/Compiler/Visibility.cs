namespace RainScript.Compiler
{
    [System.Flags]
    internal enum Visibility
    {
        None = 0,
        Public = 0x1,
        Internal = 0x2,
        Space = 0x4,
        Protected = 0x8,
        Private = 0x10,
    }
    internal static class VisibilityExtension
    {
        public static bool ContainAll(this Visibility visibility, Visibility target)
        {
            return (visibility & target) == target;
        }
        public static bool ContainAny(this Visibility visibility, Visibility target)
        {
            return (visibility & target) > 0;
        }
        public static bool Clash(this Visibility visibility, Visibility target)
        {
            if ((visibility & Visibility.Public) > 0) return ((int)target & 0b1_1111) > 0;
            else if ((visibility & Visibility.Internal) > 0) return ((int)target & 0b1_0111) > 0;
            else if ((visibility & Visibility.Space) > 0) return ((int)target & 0b1_0111) > 0;
            else if ((visibility & Visibility.Protected) > 0) return ((int)target & 0b1_1001) > 0;
            else if ((visibility & Visibility.Private) > 0) return ((int)target & 0b1_1111) > 0;
            return false;
        }
        public static bool Access(this Visibility visibility, bool space, bool child)
        {
            if (space)
            {
                if (child && ((visibility & Visibility.Protected) > 0)) return true;
                return (visibility & (Visibility.Private | Visibility.Protected)) == 0;
            }
            else
            {
                if ((visibility & Visibility.Space) > 0) return false;
                if (child && ((visibility & Visibility.Protected) > 0)) return true;
                return (visibility & (Visibility.Private | Visibility.Protected)) == 0;
            }
        }
    }
}
