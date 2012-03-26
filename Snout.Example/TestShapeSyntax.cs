using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snout.Example
{
    class TestShapeSyntax
    {
        public void Test()
        {
            var syntax = new ShapeSyntax(null);
            syntax.AddCircle.WithRadius(9).AddPolygon.Point(1, 3).Point(1, 5).Point(1, 5);
        }
    }
}
