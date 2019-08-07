using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Conan.VisualStudio.Core.VCInterfaces;
using Microsoft.VisualStudio.VCProjectEngine;

namespace Conan.VisualStudio.VCProjectWrapper
{
    class VCPropertySheetWrapper : IVCPropertySheet
    {
        private readonly VCPropertySheet _propertySheet;
        public VCPropertySheetWrapper(VCPropertySheet propertySheet)
        {
            _propertySheet = propertySheet;
        }

        public string PropertySheetFile => _propertySheet.PropertySheetFile;
    }
}
