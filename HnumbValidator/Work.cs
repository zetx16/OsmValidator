﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.Osm.PBF.Streams;

namespace HnumbValidator
{
    class Work
    {
        List<Validator> validators;

        public void Validate()
        {
            if ( Options.DownloadFromGislab )
                DownloadFiles( Options.Regions );
            else
                ReadFiles( Options.Regions );
        }

        public string[] GetDownFiles( int parts )
        {
            int n;
            var r = Reports.RegionList.StatRegions;
            var minDate = r.Min( x => x.Stamp );
            int firsIndex = 0;

            for ( int i = 0; i < r.Count; i++ )
            {
                if ( r[ i ].Stamp > minDate )
                    continue;

                if ( i > 0 && r[ i - 1 ].Stamp == minDate )
                    continue;

                if ( r[ i ].Stamp == minDate )
                    firsIndex = i;
            }

            if ( firsIndex == 0 )
                n = 85 - ( 85 / parts ) * ( parts - 1 );
            else
                n = 85 / parts;

            var res = r.Skip( firsIndex ).Take( n );
            var count = res.Count();

            if ( count < n )
            {
                var addcount = ( n - count ) > firsIndex ? firsIndex : n - count;
                res = res.Concat( r.Take( addcount ) );
            }

            var result = res.Select( x => x.Region ).ToArray();

            return result;
        }
        /*
        public void ReadFiles2( string[] regions )
        {
            string typefile = ".osm.pbf";
            WebClient webClient = new WebClient();

            if ( !Directory.Exists( Paths.DirOut ) )
                Directory.CreateDirectory( Paths.DirOut );

            if ( Options.DownloadFromGislab )
                if ( regions == null )
                    regions = GetDownFiles( 5 );
                else
                    regions = Directory.GetFiles( Paths.DirIn, "*" + typefile );


            foreach ( string region in regions )
            {
                if ( Options.DownloadFromGislab )
                {
                    Console.CursorLeft = 0;
                    Console.Write( "Download {0}", region );

                    webClient.DownloadFile( @"http://data.gis-lab.info/osm_dump/dump/latest/" + region + typefile,
                        Paths.DirIn + region + typefile + ".down" );
                    File.Delete( Paths.DirIn + region + typefile );
                    File.Move( Paths.DirIn + region + typefile + ".down", Paths.DirIn + region + typefile );

                    FileInfo file = new FileInfo( Paths.DirIn + region + typefile );
                }
                else
                {
                    FileInfo file = new FileInfo( region );
                    string region2 = file.Name.Split( '.' )[ 0 ];
                    //DateTime stump = Reports.GetRegion( region ).Stamp;


                    if ( regions != null && regions.Length > 0 && !regions.Contains( region ) )
                        continue;
                }
            }
        }
        */
        public void DownloadFiles( string[] regions )
        {
            if ( regions == null )
                regions = GetDownFiles( 5 );
            
            string typefile = ".osm.pbf";
            WebClient webClient = new WebClient();

            foreach ( string region in regions )
            {
                Console.CursorLeft = 0;
                Console.Write( "Download {0}", region );

                webClient.DownloadFile( @"http://data.gis-lab.info/osm_dump/dump/latest/" + region + typefile,
                    Paths.DirIn + region + typefile + ".down" );
                File.Delete( Paths.DirIn + region + typefile );
                File.Move( Paths.DirIn + region + typefile + ".down", Paths.DirIn + region + typefile );

                FileInfo file = new FileInfo( Paths.DirIn + region + typefile );

                Console.CursorLeft = 0;
                Console.Write( "Validate {0}", region );

#if DEBUG
                Stopwatch stw = new Stopwatch();
                stw.Start();
#endif
                ReadPbfFile( file );
#if DEBUG
                stw.Stop();
                using ( StreamWriter wr = new StreamWriter( "log_speed.txt", true ) )
                    wr.WriteLine( "{0:dd.MM.yyyy}\t{1}\t{2}", DateTime.Now, region, stw.Elapsed );
#endif

                WriteReports( region, file.LastWriteTime );

                Console.CursorLeft = 0;
                Console.Write( "Upload {0}", region );

                UploadToFtp( region );

                Console.CursorLeft = 0;
                Console.WriteLine( "Completed {0}", region );
            }
        }

        public void ReadFiles( string[] regions )
        {
            string typefile = ".osm.pbf";

            if ( !Directory.Exists( Paths.DirOut ) )
                Directory.CreateDirectory( Paths.DirOut );

            foreach ( string fl in Directory.GetFiles( Paths.DirIn, "*" + typefile ) )
            {
                FileInfo file = new FileInfo( fl );
                string region = file.Name.Split( '.' )[ 0 ];
                //DateTime stump = Reports.GetRegion( region ).Stamp;


                if ( regions != null && regions.Length > 0 && !regions.Contains( region ) )
                    continue;

                Console.CursorLeft = 0;
                Console.Write( "Validate {0}", region );
                
#if DEBUG
                Stopwatch stw = new Stopwatch();
                stw.Start();
#endif
                ReadPbfFile( file );
#if DEBUG
                stw.Stop();
                using ( StreamWriter wr = new StreamWriter( "log_speed.txt", true ) )
                    wr.WriteLine( "{0:dd.MM.yyyy}\t{1}\t{2}", DateTime.Now, region, stw.Elapsed );
#endif
                
                WriteReports( region, file.LastWriteTime );

                Console.CursorLeft = 0;
                Console.Write( "Upload {0}", region );

                UploadToFtp( region );

                Console.CursorLeft = 0;
                Console.WriteLine( "Completed {0}", region );
            }
        }

        public void ReadPbfFile( FileInfo pbfile )
        {
            validators = new List<Validator>();
            validators.Add( new ValidatorHouseNumb() );
            validators.Add( new ValidatorFlats() );
            validators.Add( new ValidatorNoStreet() );
            validators.Add( new ValidatorNames() );
            validators.Add( new ValidatorDoubleTag() );
            validators.Add( new ValidatorUncorrectTag() );
            validators.Add( new ValidatorOpeningDate() );
#if DEBUG
            validators.Add( new ValidatorNormName() );
            validators.Add( new ValidatorMapsMe() );
            validators.Add( new ValidatorNamesTop() );
#endif
            GeoCollections.ClearCollections( pbfile.Length );

            using ( FileStream fileStream = pbfile.OpenRead() )
            {
                PBFOsmStreamSource reader = new PBFOsmStreamSource( fileStream );
                foreach ( var geo in reader )
                {
                    if ( geo.Id.HasValue )
                    {
                        GeoCollections.Add( geo );
                        validators.ForEach( x => x.ValidateObject( geo ) );
                    }
                }
            }

            validators.ForEach( x => x.ValidateEndReadFile() );
        }

        public void WriteReports( string region, DateTime dateDump )
        {
            var a = validators.Select( x => new StatValidator( x.GetType().Name, x.Errors.Count ) ).ToList();
            Reports.RegionList.UpdateOrAddRegion( region, dateDump, a );
            Reports.Save();

            validators.ForEach( x => ReportHtml.SaveList( x, region, dateDump ) );
            validators.ForEach( x => ReportHtml.SaveMap( x, region ) );

            ReportHtml.SaveIndexList( validators );
        }

        public void UploadToFtp( string region )
        {
            if ( !Options.UploadToFtp )
                return;

            string ftpUrl, ftpLogin, ftpPass;

            if ( !File.Exists( "ftp.txt" ) )
            {
                Console.WriteLine( "Not found \"ftp.txt\"" );
                return;
            }

            using ( StreamReader rd = new StreamReader( "ftp.txt" ) )
            {
                try
                {
                    ftpUrl = rd.ReadLine();
                    ftpLogin = rd.ReadLine();
                    ftpPass = rd.ReadLine();
                }
                catch ( Exception ex )
                {
                    Console.WriteLine( "Error read \"ftp.txt\"" );
                    return;
                }
            }

            List<string> files = new List<string>();
            foreach ( var validator in validators.Select( x => x.FileEnd ) )
            {
                files.Add(           region + "." + validator + ".html" );
                files.Add( @"map/" + region + "." + validator + ".map.html" );
            }
            
            files.Add( "index.html" );


            foreach ( var file in files )
            {
                FileStream fileStream;
                try { fileStream = File.OpenRead( Paths.DirOut + file ); }
                catch ( FileNotFoundException ex ) { continue; }

                FtpWebRequest request = (FtpWebRequest)WebRequest.Create( ftpUrl + file );
                request.Method = WebRequestMethods.Ftp.UploadFile;
                request.Credentials = new NetworkCredential( ftpLogin, ftpPass );
                Stream ftpStream = request.GetRequestStream();

                byte[] buffer = new byte[ 1024 ];
                int bytesRead = 0;
                do
                {
                    bytesRead = fileStream.Read( buffer, 0, 1024 );
                    ftpStream.Write( buffer, 0, bytesRead );
                }
                while ( bytesRead != 0 );
                fileStream.Close();
                ftpStream.Close();
            }
        }
    }
}
