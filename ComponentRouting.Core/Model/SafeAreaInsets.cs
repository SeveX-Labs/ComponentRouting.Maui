namespace ComponentRouting.Maui.Model
{
    public class SafeAreaInsets
    {
        public double Left { get; }
        public double Right { get; }
        public double Top { get; }
        public double Bottom { get; }

        public SafeAreaInsets(double left, double top, double right, double bottom)
        {
            Left = left;
            Right = right;
            Top = top;
            Bottom = bottom;
        }
    }
}
