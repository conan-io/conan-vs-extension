using System;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Conan.VisualStudio.Services
{
    public class ErrorListService : IServiceProvider, IErrorListService
    {
        private static readonly Guid ErrorListProviderGuid = new Guid("{8F355B84-E2DE-4FE9-8482-BF7FC11A6059}");

        private static ErrorListProvider errorListProviderSingleton;

        public object GetService(Type serviceType)
        {
            return Package.GetGlobalService(serviceType);
        }

        public ErrorListProvider GetErrorListProvider()
        {
            if (errorListProviderSingleton == null)
            {
                errorListProviderSingleton = new ErrorListProvider(this)
                {
                    ProviderName = "Conan VS Extention Error List Provider",
                    ProviderGuid = ErrorListProviderGuid
                };
            }
            return errorListProviderSingleton;
        }

        private void Write(TaskErrorCategory taskErrorCategory, string text, string document)
        {
            ErrorTask task = new ErrorTask
            {
                Text = text,
                ErrorCategory = taskErrorCategory,
                Line = -1,
                Column = -1,
                Document = document,
                Category = TaskCategory.BuildCompile
            };

            if (!string.IsNullOrEmpty(task.Document))
                task.Navigate += NavigateDocument;

            GetErrorListProvider().Tasks.Add(task);
        }

        private void NavigateDocument(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (sender is Task task)
                OpenDocumentAndNavigateTo(task.Document, task.Line, task.Column);
        }

        public static void OpenDocumentAndNavigateTo(string path, int line, int column)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Package.GetGlobalService(typeof(IVsUIShellOpenDocument)) is IVsUIShellOpenDocument openDocument)
            {
                Guid logicalView = VSConstants.LOGVIEWID_Code;
                if (ErrorHandler.Failed(openDocument.OpenDocumentViaProject(path,
                    ref logicalView,
                    out Microsoft.VisualStudio.OLE.Interop.IServiceProvider sp,
                    out IVsUIHierarchy hierarchy,
                    out uint itemId,
                    out IVsWindowFrame frame)) || frame == null)
                    return;
                frame.GetProperty((int)__VSFPROPID.VSFPROPID_DocData, out object docData);
                VsTextBuffer buffer = docData as VsTextBuffer;
                if (buffer == null)
                {
                    if (docData is IVsTextBufferProvider textBufferProvider)
                    {
                        ErrorHandler.ThrowOnFailure(textBufferProvider.GetTextBuffer(out IVsTextLines lines));
                        buffer = lines as VsTextBuffer;
                        if (buffer == null)
                            return;
                    }
                }
                if (Package.GetGlobalService(typeof(VsTextManagerClass)) is IVsTextManager textManager)
                    textManager.NavigateToLineAndColumn(buffer, ref logicalView, line, column, line, column);
            }
        }

        public void Clear()
        {
            GetErrorListProvider().Tasks.Clear();
        }

        public void WriteError(string text, string document = null)
        {
            Write(TaskErrorCategory.Error, text, document);
        }

        public void WriteWarning(string text, string document = null)
        {
            Write(TaskErrorCategory.Warning, text, document);
        }

        public void WriteMessage(string text, string document = null)
        {
            Write(TaskErrorCategory.Message, text, document);
        }
    }
}
