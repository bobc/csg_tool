using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CadCommon;

namespace ConstructiveSolidGeometry
{
    public class PolygonProperties
    {
        public ColorF Color;

        public PolygonProperties()
        {
        }
        public PolygonProperties(ColorF color)
        {
            this.Color = color;
        }
    }
}
