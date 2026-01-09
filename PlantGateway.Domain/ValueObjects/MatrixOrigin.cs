using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PlantGateway.Domain.ValueObjects
{
    public enum MatrixOrigin
    {
        None,          // not present at all
        XmlInput,      // read from XML
        Derived,       // computed from parent
        Transformed,   // result of transformation
        Fallback       // default identity or synthetic
    }
}
