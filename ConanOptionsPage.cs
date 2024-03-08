using conan_vs_extension;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel;
using System.Runtime.InteropServices;
using System.Windows.Forms;

[Guid(GuidList.strConanOptionsPage)]
public class ConanOptionsPage : DialogPage
{
    private bool _enableConanExtension;
    private string _conanExecutablePath;
    private bool _useSystemConan;

    [DisplayName("Activate Conan extension")]
    [Description("Enable or disable the Conan extension")]
    public bool EnableConanExtension
    {
        get => _enableConanExtension;
        set
        {
            if (_enableConanExtension != value)
            {
                _enableConanExtension = value;
                GlobalSettings.ConanExtensionEnabled = value;
            }
        }
    }

    [DisplayName("Use System Conan")]
    [Description("Specify whether to use the Conan executable installed in the system.")]
    public bool UseSystemConan
    {
        get => _useSystemConan;
        set
        {
            if (_useSystemConan != value)
            {
                _useSystemConan = value;
                if (_useSystemConan)
                {
                    _conanExecutablePath = "conan";
                    GlobalSettings.ConanExecutablePath = "conan";
                }
                else
                {
                    _conanExecutablePath = "";
                    GlobalSettings.ConanExecutablePath = "";
                }
            }
        }
    }

    [DisplayName("Executable Path")]
    [Description("Path to the Conan executable.")]
    [Editor(typeof(ExecutablePathEditor), typeof(System.Drawing.Design.UITypeEditor))]
    public string ConanExecutablePath
    {
        get => _conanExecutablePath;
        set
        {
            _conanExecutablePath = value;
            GlobalSettings.ConanExecutablePath = value;
        }
    }

    public class ExecutablePathEditor : System.Drawing.Design.UITypeEditor
    {
        public override System.Drawing.Design.UITypeEditorEditStyle GetEditStyle(ITypeDescriptorContext context)
        {
            return System.Drawing.Design.UITypeEditorEditStyle.Modal;
        }

        public override object EditValue(ITypeDescriptorContext context, IServiceProvider provider, object value)
        {
            using (OpenFileDialog openFileDialog = new OpenFileDialog())
            {
                openFileDialog.Filter = "Executable Files (*.exe)|*.exe|All Files (*.*)|*.*";
                openFileDialog.RestoreDirectory = true;

                if (openFileDialog.ShowDialog() == DialogResult.OK)
                {
                    return openFileDialog.FileName;
                }
            }
            return value;
        }
    }
}
