using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using CadCommon;

namespace ConstructiveSolidGeometry
{
    public class Material
    {
        public int Id;

        public string name;
        public ColorF color;

        // todo: composites

        public Material()
        {

        }

        public Material(int id)
        {
            this.Id = id;
        }

        public Material clone ()
        {
            Material result = new Material();
            
            result.Id = this.Id;
            result.name = this.name;
            result.color = new ColorF(this.color);

            return result;
        }
    }
}
