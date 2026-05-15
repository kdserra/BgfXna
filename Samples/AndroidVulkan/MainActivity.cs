using Android.App;
using Android.Content.PM;
using Android.OS;
using Android.Views;

namespace BgfXna.Samples;

[Activity(
    Name = "com.bgfxna.sample.androidvulkan.MainActivity",
    Label = "BgfXna Android Vulkan Sample",
    MainLauncher = true,
    Exported = true,
    Theme = "@style/BgfXnaGameTheme",
    ConfigurationChanges = ConfigChanges.UiMode
        | ConfigChanges.Orientation
        | ConfigChanges.ScreenSize
        | ConfigChanges.Density,
    ScreenOrientation = ScreenOrientation.Portrait
)]
public class MainActivity : Activity
{
    private AndroidGameHost? _host;

    protected override void OnCreate(Bundle? savedInstanceState)
    {
        RequestWindowFeature(WindowFeatures.NoTitle);
        base.OnCreate(savedInstanceState);
        Window?.SetFlags(WindowManagerFlags.Fullscreen, WindowManagerFlags.Fullscreen);
        if (Window?.DecorView is not null)
        {
#pragma warning disable CA1422
            Window.DecorView.SystemUiFlags =
                SystemUiFlags.Fullscreen
                | SystemUiFlags.HideNavigation
                | SystemUiFlags.ImmersiveSticky
                | SystemUiFlags.LayoutFullscreen
                | SystemUiFlags.LayoutHideNavigation
                | SystemUiFlags.LayoutStable;
#pragma warning restore CA1422
        }

        _host = new AndroidGameHost(this);
        SetContentView(_host);
    }

    protected override void OnDestroy()
    {
        _host?.Stop();
        _host = null;
        base.OnDestroy();
    }
}
