using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Snout.Example
{
    /// <summary>...</summary>
    /// <builderclass name="ShapeSyntax" namespace="YourNamespace">
    ///
    ///  /* shapeList : shapeList shape
    ///             | shape ;
    ///   
    ///   shape : circle | rectangle | polygon ;
    ///   
    ///   circle : AddCircle WithRadius ;
    ///   
    ///   rectangle : AddRectangle Width Height ; */
    ///   
    ///   polygon : AddPolygon pointList Height ; 
    ///   
    ///   pointList : pointList Width Point 
    ///             | Point ;
    ///
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
