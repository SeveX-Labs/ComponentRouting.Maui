#if ANDROID
using Android.Views;
using AndroidX.Fragment.App;

namespace ComponentRouting.Maui.Chrome;

public sealed class AndroidModalWindowCandidate
{
    internal AndroidModalWindowCandidate(
        Window window,
        DialogFragment dialogFragment,
        View? decorView,
        bool matchesModalIdentity,
        bool isMauiModalNavigationFragment,
        string fragmentTypeName,
        int depth,
        string path)
    {
        Window = window;
        DialogFragment = dialogFragment;
        DecorView = decorView;
        MatchesModalIdentity = matchesModalIdentity;
        IsMauiModalNavigationFragment = isMauiModalNavigationFragment;
        FragmentTypeName = fragmentTypeName;
        Depth = depth;
        Path = path;
    }

    public Window Window { get; }
    public DialogFragment DialogFragment { get; }
    public View? DecorView { get; }
    public bool MatchesModalIdentity { get; }
    public bool IsMauiModalNavigationFragment { get; }
    public string FragmentTypeName { get; }
    public int Depth { get; }
    public string Path { get; }
}
#endif
