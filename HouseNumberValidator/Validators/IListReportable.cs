﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace HouseNumberValidator
{
    interface IListRportable
    {
        string DescriptionForList { get; }

        string GetPathList( string directory, string region );
    }
}