using System;
using System.Collections.Generic;
using System.Text;

using Lexing;

namespace FileImportExport.VRML
{
    public class VrmlLexer : GeneralLexer
    {
        public VrmlLexer()
        {

            InitMap(ref WhiteSpaceMap);
            AddMap(ref WhiteSpaceMap, WhiteSpace);

            //  setup indentifier matching by specifying *invalid* chars, then inverting map
            InitMap(ref IdentFirstMap);
            AddMapRanges(ref IdentFirstMap, "0x30-0x39, 0x0-0x20, 0x22, 0x23, 0x27, 0x2b, 0x2c, 0x2d, 0x2e, 0x5b, 0x5c, 0x5d, 0x7b, 0x7d, 0x7f");
            InvertMap(ref IdentFirstMap);

            InitMap(ref IdentRestMap);
            AddMapRanges(ref IdentRestMap, "0x0-0x20, 0x22, 0x23, 0x27, 0x2c, 0x2e, 0x5b, 0x5c, 0x5d, 0x7b, 0x7d, 0x7f");
            InvertMap(ref IdentRestMap);

            LineComment = "#";
        }

        // Initialise
        // GetToken
        // GetNextToken

    }
}
