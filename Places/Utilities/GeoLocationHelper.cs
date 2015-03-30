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
using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Resources;
using Windows.Devices.Geolocation;
using Windows.Services.Maps;

namespace Places.Utilities
{
    /// <summary>
    /// Helper class to find a geopoint location
    /// </summary>
    public class GeoLocationHelper
    {
        /// <summary>
        /// Find the address of a Geopoint location 
        /// </summary>
        /// <param name="geopoint"></param>
        /// <returns><c>address</c> upon success, <c>error message</c> otherwise</returns>
        public static async Task<string> GetAddress( Geopoint geopoint )
        {
            var loader = new ResourceLoader();
            // Find the address of the tapped location
            MapLocationFinderResult result = await MapLocationFinder.FindLocationsAtAsync( geopoint );
            // If successful then display the address.
            if( result.Status == MapLocationFinderStatus.Success )
            {
                string displayAddress = null;
                // Get the list of locations found by MapLocationFinder 
                if( result.Locations.Count > 0 )
                {
                    if( string.IsNullOrEmpty( result.Locations[ 0 ].Address.Street ) )
                    {
                        // If the address of a geographic loaction is empty or null, get only the town and region
                        var region = result.Locations[ 0 ].Address.Region != "" ? ", " + result.Locations[ 0 ].Address.Region : null;
                        displayAddress = result.Locations[ 0 ].Address.Town + region;
                    }
                    else
                    {
                        // Get the complete address of a geographic location
                        displayAddress = result.Locations[ 0 ].Address.StreetNumber + " " + result.Locations[ 0 ].Address.Street;
                    }
                    return displayAddress;
                }
                // Show message error if location is not found
                else
                {
                    var msg = loader.GetString( "AdressNotFound" );
                    return msg;
                }
            }
            // Show message error if the query location is not successful
            var error = loader.GetString( "LocationNotDefined" );
            return error;
        }
    }
}