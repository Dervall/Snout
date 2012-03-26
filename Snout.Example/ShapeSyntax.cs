using System;
using System.ComponentModel;
namespace Snout.Example
{
    [EditorBrowsable(EditorBrowsableState.Never)]
    public interface IHideShapeSyntaxObjectMembers
    {
        [EditorBrowsable(EditorBrowsableState.Never)]
        Type GetType();
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        int GetHashCode();
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        string ToString();
        
        [EditorBrowsable(EditorBrowsableState.Never)]
        bool Equals(object obj);
    }
    
    public class ShapeSyntax : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax(ShapeBuilder builder) { this.builder = builder; }
        
        ///<summary>Add a circle</summary>
        public ShapeSyntax3 AddCircle
        {
            get
            {
                builder.AddCircle();
                return new ShapeSyntax3(builder);
            }
        }
        
        ///
        public ShapeSyntax2 AddRectangle
        {
            get
            {
                builder.AddRectangle();
                return new ShapeSyntax2(builder);
            }
        }
        
        ///
        public ShapeSyntax1 AddPolygon
        {
            get
            {
                builder.AddPolygon();
                return new ShapeSyntax1(builder);
            }
        }
    }
    
    public class ShapeSyntax1 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax1(ShapeBuilder builder) { this.builder = builder; }
        
        ///
        public ShapeSyntax4 Point(Int32 x, Int32 y)
        {
            builder.AddPolygonPoint(x, y);
            return new ShapeSyntax4(builder);
        }
    }
    
    public class ShapeSyntax2 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax2(ShapeBuilder builder) { this.builder = builder; }
        
        ///
        public ShapeSyntax5 Width(Int32 width)
        {
            builder.SetRectangleWidth(width);
            return new ShapeSyntax5(builder);
        }
    }
    
    public class ShapeSyntax3 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax3(ShapeBuilder builder) { this.builder = builder; }
        
        ///<summary>Set the circle radius</summary>
        ///<param name="radius">Circle radius</param>
        public ShapeSyntax6 WithRadius(Int32 radius)
        {
            builder.SetCircleRadius(radius);
            return new ShapeSyntax6(builder);
        }
    }
    
    public class ShapeSyntax4 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax4(ShapeBuilder builder) { this.builder = builder; }
        
        ///<summary>Add a circle</summary>
        public ShapeSyntax3 AddCircle
        {
            get
            {
                builder.AddCircle();
                return new ShapeSyntax3(builder);
            }
        }
        
        ///
        public ShapeSyntax2 AddRectangle
        {
            get
            {
                builder.AddRectangle();
                return new ShapeSyntax2(builder);
            }
        }
        
        ///
        public ShapeSyntax1 AddPolygon
        {
            get
            {
                builder.AddPolygon();
                return new ShapeSyntax1(builder);
            }
        }
        
        ///
        public ShapeSyntax4 Point(Int32 x, Int32 y)
        {
            builder.AddPolygonPoint(x, y);
            return new ShapeSyntax4(builder);
        }
    }
    
    public class ShapeSyntax5 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax5(ShapeBuilder builder) { this.builder = builder; }
        
        ///
        public ShapeSyntax7 Height(Int32 height)
        {
            builder.SetRectangleHeight(height);
            return new ShapeSyntax7(builder);
        }
    }
    
    public class ShapeSyntax6 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax6(ShapeBuilder builder) { this.builder = builder; }
        
        ///<summary>Add a circle</summary>
        public ShapeSyntax3 AddCircle
        {
            get
            {
                builder.AddCircle();
                return new ShapeSyntax3(builder);
            }
        }
        
        ///
        public ShapeSyntax2 AddRectangle
        {
            get
            {
                builder.AddRectangle();
                return new ShapeSyntax2(builder);
            }
        }
        
        ///
        public ShapeSyntax1 AddPolygon
        {
            get
            {
                builder.AddPolygon();
                return new ShapeSyntax1(builder);
            }
        }
    }
    
    public class ShapeSyntax7 : IHideShapeSyntaxObjectMembers
    {
        private readonly ShapeBuilder builder;
        
        internal ShapeSyntax7(ShapeBuilder builder) { this.builder = builder; }
        
        ///<summary>Add a circle</summary>
        public ShapeSyntax3 AddCircle
        {
            get
            {
                builder.AddCircle();
                return new ShapeSyntax3(builder);
            }
        }
        
        ///
        public ShapeSyntax2 AddRectangle
        {
            get
            {
                builder.AddRectangle();
                return new ShapeSyntax2(builder);
            }
        }
        
        ///
        public ShapeSyntax1 AddPolygon
        {
            get
            {
                builder.AddPolygon();
                return new ShapeSyntax1(builder);
            }
        }
    }
    
}
