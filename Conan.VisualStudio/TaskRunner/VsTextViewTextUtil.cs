using System;
using System.Text;
using EnvDTE;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.TextManager.Interop;

namespace Conan.VisualStudio.TaskRunner
{
    internal class VsTextViewTextUtil : ITextUtil
    {
        private int _currentLineLength;
        private int _lineNumber;
        private readonly IVsTextView _view;

        public VsTextViewTextUtil(IVsTextView view)
        {
            _view = view;
        }

        public Range CurrentLineRange
        {
            get { return new Range { LineNumber = _lineNumber, LineRange = new LineRange { Start = 0, Length = _currentLineLength } }; }
        }

        public bool Delete(Range range)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                GetEditPointForRange(range)?.Delete(range.LineRange.Length);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool Insert(Range position, string text, bool addNewline)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            try
            {
                GetEditPointForRange(position)?.Insert(text + (addNewline ? Environment.NewLine : string.Empty));
                return true;
            }
            catch
            {
                return false;
            }
        }

        public bool TryReadLine(out string line)
        {
            int hr = _view.GetBuffer(out IVsTextLines textLines);

            if (hr != VSConstants.S_OK || textLines == null)
            {
                line = null;
                return false;
            }

            hr = textLines.GetLineCount(out int lineCount);

            if (hr != VSConstants.S_OK || _lineNumber == lineCount)
            {
                line = null;
                return false;
            }

            int lineNumber = _lineNumber++;
            hr = textLines.GetLengthOfLine(lineNumber, out _currentLineLength);

            if (hr != VSConstants.S_OK)
            {
                line = null;
                return false;
            }

            hr = textLines.GetLineText(lineNumber, 0, lineNumber, _currentLineLength, out line);

            if (hr != VSConstants.S_OK)
            {
                line = null;
                return false;
            }

            var lineData = new LINEDATA[1];
            textLines.GetLineData(lineNumber, lineData, null);
            if (lineData[0].iEolType != EOLTYPE.eolNONE)
            {
                line += "\n";
            }

            return true;
        }

        public string ReadAllText()
        {
            var text = new StringBuilder();
            while (TryReadLine(out string line))
            {
                text.Append(line);
            }
            return text.ToString();
        }

        public void Reset()
        {
            _currentLineLength = 0;
            _lineNumber = 0;
        }

        private EditPoint GetEditPointForRange(Range range)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            int hr = _view.GetBuffer(out IVsTextLines textLines);

            if (hr != VSConstants.S_OK || textLines == null)
            {
                return null;
            }

            hr = textLines.CreateEditPoint(range.LineNumber, range.LineRange.Start, out object editPointObject);

            if (hr != VSConstants.S_OK || !(editPointObject is EditPoint editPoint))
            {
                return null;
            }

            return editPoint;
        }

        public void FormatRange(LineRange range)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            Reset();
            this.GetExtentInfo(range.Start, range.Length, out int startLine, out int startLineOffset, out int endLine, out int endLineOffset);

            _view.GetSelection(out int oldStartLine, out int oldStartLineOffset, out int oldEndLine, out int oldEndLineOffset);
            _view.SetSelection(startLine, startLineOffset, endLine, endLineOffset);
            var target = (IOleCommandTarget)ServiceProvider.GlobalProvider.GetService(typeof(SUIHostCommandDispatcher));
            if (null == target)
                return;
            Guid cmdid = VSConstants.VSStd2K;
            _ = _view.SendExplicitFocus();
            _ = target.Exec(ref cmdid, (uint)VSConstants.VSStd2KCmdID.FORMATSELECTION, 0, IntPtr.Zero, IntPtr.Zero);
            _view.SetSelection(oldStartLine, oldStartLineOffset, oldEndLine, oldEndLineOffset);
        }
    }
}
