namespace Netch.Models
{
    public readonly struct NumberRange
    {
        public int Start { get; }

        public int End { get; }

        public NumberRange(int start, int end)
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