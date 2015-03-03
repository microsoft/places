/*	
The MIT License (MIT)
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
using Windows.UI.Xaml.Controls;
using Lumia.Sense;
using System.Globalization;

/// <summary>
/// The User Control item template is documented at http://go.microsoft.com/fwlink/?LinkId=234236
/// </summary>
namespace Places
{
    /// <summary>
    /// class to display the popup(pushpin) on the map.
    /// </summary>
    public partial class PushPin : UserControl
    {
        /// <summary>
        /// holds address of the place.
        /// </summary>
        private string _placeKind;
        /// <summary>
        /// Place Instance
        /// </summary>
        private Place _place ;
        /// <summary>
        /// constructor
        /// </summary>
        public PushPin(Place place, string placeKind)
        {
            InitializeComponent();
            _place = place;
            _placeKind = placeKind;
            Loaded += PushPin_Loaded;
        }

        // <summary>
        // page load event. sets the pushpin text.
        // </summary>
        // <param name="sender">event details</param>
        // <param name="e">event sender</param>
        void PushPin_Loaded( object sender, RoutedEventArgs e )
        {
            Lbltext.Text = "'" + _placeKind + "'" + " place";
        }

        /// <summary>
        /// Tapped event on the pushpin to display details.
        /// </summary>
        /// <param name="sender">event details</param>
        /// <param name="e">event sender</param>
        private void PushPinTapped( object sender, Windows.UI.Xaml.Input.TappedRoutedEventArgs e )
        {
            var frame = Window.Current.Content as Frame;
            var resultStr = _place.Kind + "\n" + _place.Position.Latitude.ToString() + "\n"+_place.Position.Longitude.ToString() + "\n" +
                _place.Radius.ToString() +"\n" + _place.LengthOfStay.ToString() + "\n" + _place.TotalLengthOfStay.ToString() + "\n" + _place.TotalVisitCount.ToString();
            frame.Navigate(typeof(PivotPage),resultStr);
        }
    }
}