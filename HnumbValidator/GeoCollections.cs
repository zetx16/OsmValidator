﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.Osm;

namespace HnumbValidator
{
    static class GeoCollections
    {
        public static List<Error> members;
        public static List<List<NdCoord>> noods;
        public static List<WayCoord> ways;
        public static List<RelCoord> rels;
        private const int nodesInOneList = 2000000;


        public static void ClearCollections( long filesize )
        {
            members = new List<Error>();
            ways = null;
            rels = null;
            noods = null;

            GC.Collect( 2, GCCollectionMode.Forced, true );

            noods = new List<List<NdCoord>>();
            noods.Add( new List<NdCoord>( nodesInOneList ) );

            //nds = new List<NdCoord>( (int)( filesize / 7 * 1.2 ) );
            ways = new List<WayCoord>( (int)( filesize / 50 * 1.2 ) );
            rels = new List<RelCoord>( (int)( filesize / 1100 * 1.2 ) );
        }

        public static void Add( OsmGeo geo )
        {
            if ( geo.Type == OsmGeoType.Node )
            {
                if ( noods[ noods.Count - 1 ].Count == nodesInOneList )
                    noods.Add( new List<NdCoord>( nodesInOneList ) );
                try
                {
                    noods[ noods.Count - 1 ].Add( new NdCoord( (long)geo.Id, (float)( (Node)geo ).Coordinate.Latitude, (float)( (Node)geo ).Coordinate.Longitude ) );
                }
                catch ( OutOfMemoryException ex )
                {
                    Console.WriteLine( "{0}: {1}", ex.Source, ex.Message );
                }
            }
            else if ( geo.Type == OsmGeoType.Way )
            {
                try
                {
                    var way = (Way)geo;
                    if ( way.Nodes.First() != way.Nodes.Last() )
                        ways.Add( new WayCoord( (long)geo.Id, way.Nodes[ 0 ] ) );
                    else
                    {
                        var center = GetCenter( way );
                        ways.Add( new WayCoord( (long)geo.Id, way.Nodes[ 0 ], center.lat, center.lon ) );
                    }
                }
                catch ( OutOfMemoryException ex )
                {
                    Console.WriteLine( "{0}: {1}", ex.Source, ex.Message );
                }
            }
            else if ( geo.Type == OsmGeoType.Relation )
            {
                Relation rel = (Relation)geo;
                rels.Add( new RelCoord( (long)geo.Id, (long)rel.Members[ 0 ].MemberId, (OsmGeoType)rel.Members[ 0 ].MemberType ) );

                if ( geo.Tags.ContainsKeyValue( "type", "associatedStreet" ) )
                {
                    foreach ( var m in rel.Members )
                    {
                        if ( m.MemberRole == "house" )
                        {
                            members.Add( new Error( (OsmGeoType)m.MemberType, (long)m.MemberId ) );
                        }
                    }
                }
            }
        }

        public static string GetNotes( OsmSharp.Collections.Tags.TagsCollectionBase tags )
        {
            string result = "";
            foreach ( var tag in tags )
            {
                if ( tag.Key == "note" )
                {
                    result += "<b>Note:</b> " + tag.Value + "<br>";
                    continue;
                }
                if ( tag.Key == "fixme" )
                {
                    result += "<b>FixMe:</b> " + tag.Value + "<br>";
                    continue;
                }
                /*
                if ( tag.Key == "description" )
                {
                    result += "<b>Description:</b> " + tag.Value + "<br>";
                    continue;
                }
                */
            }
            return result;
        }

        public static void GetCoordinates( OsmGeo geo, Error res )
        {
            if ( geo.Type == OsmGeoType.Node )
            {
                res.lat = (float)( (Node)geo ).Coordinate.Latitude;
                res.lon = (float)( (Node)geo ).Coordinate.Longitude;
            }
            else if ( geo.Type == OsmGeoType.Way )
            {
                GetCoordinatesWay( (long)geo.Id, res );
            }
            else if ( geo.Type == OsmGeoType.Relation )
            {
                var member = ( (Relation)geo ).Members[ 0 ];
                if ( member.MemberType == OsmGeoType.Node )
                {
                    long ndid = (long)member.MemberId;
                    GetCoordinatesNode( ndid, res );
                }
                else if ( member.MemberType == OsmGeoType.Way )
                {
                    long ndid = (long)member.MemberId;
                    GetCoordinatesWay( ndid, res );
                }
                else if ( member.MemberType == OsmGeoType.Relation )
                {
                    long mbid = (long)member.MemberId;
                    OsmGeoType mbtp = (OsmGeoType)member.MemberType;
                    GetCoordinatesRel( mbid, mbtp, res );
                }
            }
        }

        private static NdCoord GetCenter( Way way )
        {
            float slat = 0.0f, slon = 0.0f;
            int count = 0;
            foreach ( var node in way.Nodes )
            {
                var nd = GetNode( node );
                if ( nd.id == -1 )
                    continue;
                slat += nd.lat;
                slon += nd.lon;
                count++;
            }

            return new NdCoord( 0, slat / count, slon / count );
        }
        private static NdCoord GetNode( long id )
        {
            foreach ( var ndlist in noods )
            {
                if ( ndlist[ ndlist.Count - 1 ].id < id )
                    continue;

                int i = ndlist.BinarySearch( new NdCoord( id ) );
                if ( i >= 0 )
                {
                    return ndlist[ i ];
                }
            }
            return new NdCoord( -1 );
        }

        private static void GetCoordinatesNode( long id, Error res )
        {
            foreach ( var ndlist in noods )
            {
                if ( ndlist[ ndlist.Count - 1 ].id < id )
                    continue;

                int i = ndlist.BinarySearch( new NdCoord( id ) );
                if ( i >= 0 )
                {
                    res.lat = ndlist[ i ].lat;
                    res.lon = ndlist[ i ].lon;
                }
            }
        }
        private static void GetCoordinatesWay( long id, Error res )
        {
            int w = ways.BinarySearch( new WayCoord( id ) );
            if ( w >= 0 )
            {
                if ( !float.IsNaN( ways[ w ].lat ) )
                {
                    res.lat = ways[ w ].lat;
                    res.lon = ways[ w ].lon;
                }
                else
                {
                    long ndid = ways[ w ].ndId;
                    GetCoordinatesNode( ndid, res );
                }
            }
        }
        private static void GetCoordinatesRel( long id, OsmGeoType type, Error res )
        {
        }

        public static bool CountryRu( OsmGeo geo )
        {
            string country;
            if ( geo.Tags.TryGetValue( "addr:country", out country ) && country != "RU" )
                return false;

            string street;
            if ( geo.Tags.TryGetValue( "addr:street", out street ) )
            {
                bool ru = false;
                foreach ( var ch in street )
                    if ( ( ch >= 'а' && ch <= 'я' ) || ( ch >= 'А' && ch <= 'Я' ) )
                    {
                        ru = true;
                        break;
                    }
                if ( !ru )
                    return false;
            }
            if ( geo.Tags.TryGetValue( "addr:city", out street ) )
            {
                bool ru = false;
                foreach ( var ch in street )
                    if ( ( ch >= 'а' && ch <= 'я' ) || ( ch >= 'А' && ch <= 'Я' ) )
                    {
                        ru = true;
                        break;
                    }
                if ( !ru )
                    return false;
            }
            if ( geo.Tags.TryGetValue( "addr:place", out street ) )
            {
                bool ru = false;
                foreach ( var ch in street )
                    if ( ( ch >= 'а' && ch <= 'я' ) || ( ch >= 'А' && ch <= 'Я' ) )
                    {
                        ru = true;
                        break;
                    }
                if ( !ru )
                    return false;
            }

            return true;
        }

        public static int Distance( Error c1, Error c2 )
        {
            int R = 6371 * 1000;
            var sin = Sin( c1.lat ) * Sin( c2.lat );
            var cos = Cos( c1.lat ) * Cos( c2.lat ) * Cos( c1.lon - c2.lon );
            double result = R * Math.Acos( sin + cos );
            return (int)result;
        }
        private static double Sin( double grad )
        {
            return Math.Sin( ToRad( grad ) );
        }
        private static double Cos( double grad )
        {
            return Math.Cos( ToRad( grad ) );
        }
        private static double ToRad( double grad )
        {
            return grad * Math.PI / 180;
        }
    }
}