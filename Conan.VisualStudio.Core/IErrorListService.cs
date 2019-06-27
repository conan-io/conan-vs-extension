namespace Conan.VisualStudio.Core
{
    public interface IErrorListService
    {
        void Clear();
        void WriteError(string text, string document = null);
        void WriteWarning(string text, string document = null);
        void WriteMessage(string text, string document = null);
    }
}
