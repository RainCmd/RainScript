namespace RainScript.Compiler.LogicGenerator
{
    [System.Flags]
    internal enum SplitFlag
    {
        Bracket0 = 0x1,
        Bracket1 = 0x2,
        Bracket2 = 0x4,
        Comma = 0x8,
        Assignment = 0x10,
        Question = 0x20,
        Colon = 0x40,
        Lambda = 0x80,
    }
    internal static class SplitFlagExtension
    {
        public static bool ContainAny(this SplitFlag flag, SplitFlag target)
        {
            return (flag & target) > 0;
        }
    }
}
