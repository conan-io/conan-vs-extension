using conan_vs_extension;
using Microsoft.VisualStudio.Shell;
using System.ComponentModel;
using System.Runtime.InteropServices;

[Guid(GuidList.strConanOptionsPage)]
public class ConanOptionsPage : DialogPage
{
    private string _conanExecutablePath;
    private bool _useSystemConan;

    [DisplayName("Executable Path")]
    [Description("Path to the Conan executable.")]
    public string ConanExecutablePath
    {
        get
        {
            return _conanExecutablePath;
        }
        set
        {
            _useSystemConan = false;
            _conanExecutablePath = value;
            GlobalSettings.ConanExecutablePath = _conanExecutablePath;
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
                    _conanExecutablePath = "System";
                }
            }
        }
    }
}
