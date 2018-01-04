namespace Conan.VisualStudio.Services
{
    public interface IDialogService
    {
        bool ShowOkCancel(string text);
        void ShowPluginError(string error);
    }
}
