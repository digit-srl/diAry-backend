using Geohash;
using MongoDB.Driver.GeoJsonObjectModel;
using Newtonsoft.Json;
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

        public static string ToGeoJson(this GeoJsonGeometry<GeoJson2DGeographicCoordinates> geometry) {
            var geoJson = new OutputModels.CallToActionMatch.GeoJsonGeometry {
                Type = "Polygon",
                Coordinates = geometry.ToPolygonArray()
            };
            return JsonConvert.SerializeObject(geoJson);
        }

        public static GeoJsonPolygon<GeoJson2DGeographicCoordinates> PolygonFromGeoJson(this string geojson) {
            var obj = JsonConvert.DeserializeObject<OutputModels.CallToActionMatch.GeoJsonGeometry>(geojson);
            if(!obj.Type.Equals("Polygon")) {
                return null;
            }

            return GeoJson.Polygon(
                (from p in obj.Coordinates[0]
                 select new GeoJson2DGeographicCoordinates(p[0], p[1])).ToArray()
            );
        }

        public static Task<List<string>> GenerateCoveringGeohashes(this Geohasher hasher, GeoJsonPolygon<GeoJson2DGeographicCoordinates> polygon, int precision = 4) {
            var outerRing = new NetTopologySuite.Geometries.LinearRing(
                (from coord in polygon.Coordinates.Exterior.Positions
                 select new NetTopologySuite.Geometries.Coordinate(coord.Latitude, coord.Longitude)).ToArray()
            );
            var p = new NetTopologySuite.Geometries.Polygon(outerRing);
            return hasher.GetHashesAsync(p, precision, Mode.Intersect);
        }

    }

}
