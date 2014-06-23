using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Lumia.Sense;

namespace Places.Utilities
{
    public static class MapExtensions
    {
        private static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
          "Place",
          typeof(Place),
          typeof(MapExtensions),
          new PropertyMetadata(default(uint)));
        private static void SetValueProperty(DependencyObject element, Place value)
        {
            element.SetValue(ValueProperty, value);
        }
        private static Place GetValueProperty(DependencyObject element)
        {
            return (Place)element.GetValue(ValueProperty);
        }

        public static void SetValue(MapElement target, Place value)
        {
            SetValueProperty(target, value);
        }

        public static Place GetValue(MapElement target)
        {
            return GetValueProperty(target);
        }
    }
}
