﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using OsmSharp.Osm;

namespace HnumbValidator
{
    public class ValidatorUncorrectTag : Validator
    {
        Dictionary<List<string>, Dictionary<string, string>> tags;

        public ValidatorUncorrectTag()
        {
            errors = new List<Error>();

            FileEnd = "uncorrect";
            Title = "Не те теги";

            descriptionForList = "Определение типа объекта по названию и отображение недостающих тегов";
            descriptionForMap = "Определение типа объекта по названию и отображение недостающих тегов";

            tags = new Dictionary<List<string>, Dictionary<string, string>>
            {
                {
                    new List<string>{
                        "детский дом",
                        "детдом",
                        "дет дом"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "social_facility" },
                        { "social_facility", "group_home" },
                        { "social_facility:for", "orphan" }
                    }
                },
                {
                    new List<string>{
                        "престарелых"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "social_facility" },
                        { "social_facility", "group_home" },
                        { "social_facility:for", "senior" }
                    }
                },
                {
                    new List<string>{
                        "молочная кухня"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "social_facility" },
                        { "social_facility", "dairy_kitchen" }
                    }
                },
                {
                    new List<string>{
                        "детский сад",
                        "доу",
                        "детсад"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "kindergarten" }
                    }
                },
                {
                    new List<string>{
                        "школа искусств",
                        "школа исскуств",
                        "школа художественная",
                        "художественная школа"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "training" },
                        { "training", "art" }
                    }
                },
                {
                    new List<string>{
                        "музыкальная школа"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "training" },
                        { "training", "music" }
                    }
                },
                {
                    new List<string>{
                        "спортивная школа"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "training" },
                        { "training", "sport" }
                    }
                },
                {
                    new List<string>{
                        "школа",
                        "сош"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "school" }
                    }
                },
                {
                    new List<string>{
                        "загс"
                    },
                    new Dictionary<string,string>{
                        { "office", "government" },
                        { "government", "register_office" }
                    }
                },
                {
                    new List<string>{
                        "прокуратура",
                        "прокурор"
                    },
                    new Dictionary<string,string>{
                        { "office", "government" },
                        { "government", "prosecutor" }
                    }
                },
                {
                    new List<string>{
                        "министерство"
                    },
                    new Dictionary<string,string>{
                        { "office", "government" },
                        { "government", "ministry" }
                    }
                },
                {
                    new List<string>{
                        "нотариус",
                        "нотариальная"
                    },
                    new Dictionary<string,string>{
                        { "office", "lawyer" },
                        { "lawyer", "notary" }
                    }
                },
                {
                    new List<string>{
                        "адвокат"
                    },
                    new Dictionary<string,string>{
                        { "office", "lawyer" },
                        { "lawyer", "advocate" }
                    }
                },
                {
                    new List<string>{
                        "налоговая"
                    },
                    new Dictionary<string,string>{
                        { "office", "tax" }
                    }
                },
                {
                    new List<string>{
                        "центр занятости"
                    },
                    new Dictionary<string,string>{
                        { "office", "employment_agency" }
                    }
                },
                {
                    new List<string>{
                        "нии"
                    },
                    new Dictionary<string,string>{
                        { "office", "research" }
                    }
                },
                {
                    new List<string>{
                        "мэрия"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "townhall" }
                    }
                },
                {
                    new List<string>{
                        "дк",
                        "дом культуры",
                        "рдк"
                    },
                    new Dictionary<string,string>{
                        { "amenity", "community_centre" }
                    }
                },
                {
                    new List<string>{
                        "лагерь"
                    },
                    new Dictionary<string,string>{
                        { "leisure", "resort" },
                        { "resort", "kids_camp" }
                    }
                },
                {
                    new List<string>{
                        "санаторий"
                    },
                    new Dictionary<string,string>{
                        { "leisure", "resort" },
                        { "resort", "sanatorium" }
                    }
                },
                {
                    new List<string>{
                        "пансионат"
                    },
                    new Dictionary<string,string>{
                        { "leisure", "resort" },
                        { "resort", "pension" }
                    }
                },
                {
                    new List<string>{
                        "база отдыха",
                        "б.о.",
                        "б/о"
                    },
                    new Dictionary<string,string>{
                        { "leisure", "resort" },
                        { "resort", "recreation_center" }
                    }
                },
                {
                    new List<string>{
                        "баня",
                        "сауна"
                    },
                    new Dictionary<string,string>{
                        { "leisure", "sauna" }
                    }
                }
            };
        }

        public override void ValidateObject( OsmGeo geo )
        {
            string value;
            if ( !geo.Tags.TryGetValue( "name", out value ) )
                return;

            foreach (var keyvalue in tags)
            {
                if ( value.ToLower().ContainsOneOf( keyvalue.Key ) )
                {
                    if (geo.Tags.ContainsKeyValue("public_transport", "platform") ||
                        geo.Tags.ContainsKeyValue("highway", "bus_stop") ||
                        geo.Tags.ContainsKeyValue("public_transport", "stop_position") ||
                        geo.Tags.ContainsKeyValue("type", "route") ||
                        geo.Tags.ContainsKeyValue("type", "route_master") ||
                        geo.Tags.ContainsKeyValue("type", "public_transport"))
                        return;

                    var error = new Error( geo, value );
                    foreach ( var tag in keyvalue.Value )
                        if ( !geo.Tags.ContainsKeyValue( tag.Key, tag.Value ) )
                            error.Description += tag.Key + " = " + tag.Value + "<br>";

                    if ( !error.Description.IsEmpty() )
                        errors.Add( error );

                    return;
                }
            }

        }

        public override void ValidateEndReadFile()
        {
            errors = errors.OrderBy(x => x.Description).ThenByDescending(x => x.TimeStump).ToList();
        }
    }
}