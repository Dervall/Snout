using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snout.Example
{
    /// <summary>...</summary>
    /// <builderclass name="ShapeSyntax" namespace="Snout.Example">
    /// (AddCircle WithRadius | AddRectangle Width Height | AddPolygon Point+ ) +
    /// </builderclass>
    public class ShapeBuilder
    {
        /// <summary>Add a circle</summary>
        /// <buildermethod/>
        public void AddCircle() {  }

        /// <summary>Set the circle radius</summary>
        /// <param name="radius">Circle radius</param>
        /// <buildermethod name="WithRadius"/>
        public void SetCircleRadius(int radius) {  }

        // Omitted summary and params for brevity on the rest of the 
        // methods.

        /// <buildermethod/>
        public void AddRectangle() {  }

        /// <buildermethod name="Width"/>
        public void SetRectangleWidth(int width) {  }

        /// <buildermethod name="Height"/>
        public void SetRectangleHeight(int height) {  }

        /// <buildermethod/>
        public void AddPolygon() {  }

        /// <buildermethod name="Point"/>
        public void AddPolygonPoint(int x, int y) {  }
    }
}
