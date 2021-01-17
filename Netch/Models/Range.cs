namespace Netch.Models
{
    public readonly struct Range
    {
        public int Start { get; }

        public int End { get; }

        public Range(int start, int end)
        {
            Start = start;
            End = end;
        }

        public bool InRange(int num)
        {
            return Start <= num && num <= End;
        }
    }
}