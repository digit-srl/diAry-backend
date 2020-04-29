using MongoDB.Driver.GeoJsonObjectModel;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace DiaryCollector {
    
    public static class GeoExtensions {

        public static double[][][] ToPolygonArray(this GeoJsonGeometry<GeoJson2DGeographicCoordinates> geometry) {
            if(!(geometry is GeoJsonPolygon<GeoJson2DGeographicCoordinates>)) {
                throw new ArgumentException();
            }
            var polygon = (GeoJsonPolygon<GeoJson2DGeographicCoordinates>)geometry;

            var extRing = polygon.Coordinates.Exterior;
            var positions = from coord in extRing.Positions
                            select new double[] {
                                coord.Longitude, coord.Latitude
                            };

            return new double[1][][] {
                positions.ToArray()
            };
        }

        public static double[][] ToRingArray(this GeoJsonGeometry<GeoJson2DGeographicCoordinates> geometry) {
            if (!(geometry is GeoJsonPolygon<GeoJson2DGeographicCoordinates>)) {
                throw new ArgumentException();
            }
            var polygon = (GeoJsonPolygon<GeoJson2DGeographicCoordinates>)geometry;

            var extRing = polygon.Coordinates.Exterior;
            var positions = from coord in extRing.Positions
                            select new double[] {
                                coord.Longitude, coord.Latitude
                            };

            return positions.ToArray();
        }

    }

}
