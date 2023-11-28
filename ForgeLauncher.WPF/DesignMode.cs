using System.ComponentModel;
using System.Windows;

namespace ForgeLauncher.WPF;

public static class DesignMode
{
    #region DesignMode detection, thanks to Galasoft

    private static bool? _isInDesignMode;

    public static bool IsInDesignModeStatic
    {
        get
        {
            if (!_isInDesignMode.HasValue)
            {
                var prop = DesignerProperties.IsInDesignModeProperty;
                _isInDesignMode
                    = (bool)DependencyPropertyDescriptor
                        .FromProperty(prop, typeof(FrameworkElement))
                        .Metadata.DefaultValue;
            }

            return _isInDesignMode.Value;
        }
    }

    #endregion
}
