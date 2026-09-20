namespace Snap.Core.Tray;

public static class TrayActivationPolicy
{
    public static bool ShouldOpenVault(bool isLeftButton, int clickCount) =>
        isLeftButton && clickCount == 2;
}
