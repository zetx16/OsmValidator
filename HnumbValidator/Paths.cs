﻿using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HnumbValidator
{
    static class Paths
    {
        static string shortDirIn      = @"\";
        static string shortDirOut     = @"\log\";
        static string shortDirOutTemp = @"\log\temp\";
        static string shortDirOutMap  = @"\log\map\";

        static string fileReport      = @"Report.txt";
        static string fileReportXml   = @"Report.xml";

        static string dirIn;
        static string dirOut;
        static string dirOutTemp;
        static string dirOutMap;


        public static string DirIn          { get { return dirIn; } }
        public static string DirOut         { get { return dirOut; } }
        public static string DirOutTemp     { get { return dirOutTemp; } }
        public static string DirOutMap      { get { return dirOutMap; } }

        public static string FileReport     { get { return fileReport; } }
        public static string FileReportXml  { get { return fileReportXml; } }

        static Paths()
        {
            dirIn       = ( Options.DirectoryInput  + shortDirIn ).Replace( "\\\\", "\\" );
            dirOut      = ( Options.DirectoryOutput + shortDirOut ).Replace( "\\\\", "\\" );
            dirOutTemp  = ( Options.DirectoryOutput + shortDirOutTemp ).Replace( "\\\\", "\\" );
            dirOutMap   = ( Options.DirectoryOutput + shortDirOutMap ).Replace( "\\\\", "\\" );
            
            if ( !Directory.Exists( dirOut ) )
                Directory.CreateDirectory( dirOut );
            if ( !Directory.Exists( dirOutTemp ) )
                Directory.CreateDirectory( dirOutTemp );
            if ( !Directory.Exists( dirOutMap ) )
                Directory.CreateDirectory( dirOutMap );
        }
    }
}
