/*	
Copyright (c) 2015 Microsoft

Permission is hereby granted, free of charge, to any person obtaining a copy
of this software and associated documentation files (the "Software"), to deal
in the Software without restriction, including without limitation the rights
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell
copies of the Software, and to permit persons to whom the Software is
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY,
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING FROM,
OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS IN
THE SOFTWARE. 
 */
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls.Maps;
using Lumia.Sense;

namespace Places.Utilities
{
    /// <summary>
    ///  Extension library for maps.
    /// </summary>
    public static class MapExtensions
    {
        /// <summary>
        /// Dependency Property instance
        /// </summary>
        private static readonly DependencyProperty ValueProperty = DependencyProperty.RegisterAttached(
          "Place",
          typeof(Place),
          typeof(MapExtensions),
          new PropertyMetadata(default(uint)));

        /// <summary>
        /// Set the value of a property
        /// </summary>
        /// <param name="element">Represents an object that participates in the dependency property system. The object whose property value will be set.</param>
        /// <param name="value">The new property value.</param>
        private static void SetValueProperty(DependencyObject element, Place value)
        {
            element.SetValue(ValueProperty, value);
        }

        /// <summary>
        /// Get value of property
        /// </summary>
        /// <param name="element">The object whose property value will be returned</param>
        /// <returns>Place object</returns>
        private static Place GetValueProperty(DependencyObject element)
        {
            return (Place)element.GetValue(ValueProperty);
        }

        /// <summary>
        /// Set value for a target object
        /// </summary>
        /// <param name="target">Represents an element on the map control. The object whose property value will be set</param>
        /// <param name="value">The new property value.</param>
        public static void SetValue(MapElement target, Place value)
        {
            SetValueProperty(target, value);
        }

        /// <summary>
        /// Get value for target oject
        /// </summary>
        /// <param name="target">The object whose property value will be returned</param>
        /// <returns>Place object</returns>
        public static Place GetValue(MapElement target)
        {
            return GetValueProperty(target);
        }
    }
}