namespace Conan.VisualStudio.Services
{
    public interface IDialogService
    {
        void ShowInfo(string text);
        bool ShowOkCancel(string text);
        void ShowPluginError(string error);
    }
}
