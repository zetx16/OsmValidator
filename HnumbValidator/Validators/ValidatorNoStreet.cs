﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.Osm;
using System.Collections;

namespace HnumbValidator
{
    class ValidatorNoStreet: Validator
    {
        List<string> addrkeys;

        public ValidatorNoStreet()
        {
            FileEnd = "nostreet";
            Title = "Нет улицы";

            descriptionForList = @"Адреса без ""addr:street"" и ""addr:place""";
            descriptionForMap = descriptionForList + "<br><br>"
                        + @"<div class=""info-colour"" style=""background-color:orange;""></div> - есть ""addr:city"" или ""addr:suburb""<br>";

            errors = new List<Error>();
            addrkeys = new List<string>{
				"addr:street",
				"addr:place",
                //"addr:quarter",
                //"addr:neighbourhood",
                "is_in:neighbourhood"
			};
        }

        public override IEnumerable GetTableHead()
        {
            yield return "Номер дома";
            yield return "Доп. информация";
        }

        public override void ValidateObject( OsmGeo geo )
        {
            string value;
            if ( geo.Tags.TryGetValue( "addr:housenumber", out value ) && !geo.Tags.ContainsOneOfKeys( addrkeys ) )
            {
                if ( !GeoCollections.CountryRu( geo ) )
                    return;

                Error nostreet = new Error( geo, value );

                string city;

                if ( geo.Tags.TryGetValue( "addr:city", out city ) )
                    nostreet.Description += "<font color=\"gray\">city:</font> " + city;
                if ( geo.Tags.TryGetValue( "addr:suburb", out city ) )
                    if ( nostreet.Description != String.Empty )
                        nostreet.Description += "  |  " + "<font color=\"gray\">suburb: </font>" + city;
                    else
                        nostreet.Description += "<font color=\"gray\">suburb: </font>" + city;

                if ( nostreet.Description != string.Empty )
                    nostreet.Level = ErrorLevel.Level5;

                errors.Add( nostreet );
            }

            string street, place;
            if ( geo.Tags.TryGetValue( "addr:place", out place ) && geo.Tags.TryGetValue( "addr:street", out street ) )
            {
                if ( street == place )
                    return;

                Error error = new Error( geo, place + " | " + street );
                error.Description = "Разные addr:place и addr:street";
                error.Level = ErrorLevel.Level2;

                errors.Add( error );
            }
        }

        public override void ValidateEndReadFile()
        {
            errors = errors.Except( GeoCollections.members ).ToList();
            errors = errors.OrderByDescending( x => x.TimeStump ).OrderBy( x => x.Description ).ToList();
        }
    }
}
