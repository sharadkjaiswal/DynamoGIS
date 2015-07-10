using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using EGIS.ShapeFileLib;

namespace EGIS.DynamoLib
{
    internal struct PointXYZ
    {
        public double X;
        public double Y;
        public double Z;
    }

    /// <summary>
    /// Represents the shape read from ShapeFile
    /// </summary>
    public class Shape
    {
        /// <summary>
        /// Represents raw shape data
        /// </summary>
        private IEnumerable<PointXYZ[]>[] ShapeData { get; set; }
        
        /// <summary>
        /// Constructs a ShapeFile using a path to a .shp shape file
        /// </summary>
        /// <param name="shapeFilePath">The path to the ".shp" shape file</param>
        private Shape(string shapeFilePath)
        {
            using (var file = new ShapeFile(shapeFilePath))
            {
                RecordCount = file.RecordCount;
                ShapeData = new IEnumerable<PointXYZ[]>[RecordCount];
                for (int i = 0; i < RecordCount; ++i )
                {
                    var shapeData = file.GetShapeDataD(i);
                    ShapeData[i] = shapeData.Select(x => x.Select(p => new PointXYZ() { X = p.X, Y = p.Y, Z = 0 }).ToArray());
                }
            }
        }

        /// <summary>
        /// Loads the shape file(*.shp) from the given path
        /// </summary>
        /// <param name="shapeFilePath">Full path for the shape file</param>
        /// <returns>Shape object</returns>
        public static Shape LoadFromFile(string shapeFilePath)
        {
            return new Shape(shapeFilePath);
        }

        /// <summary>
        /// Gets number of shapes
        /// </summary>
        public int RecordCount { get; private set; }

        /// <summary>
        /// Gets list of shape polygons at a given record index.
        /// </summary>
        /// <param name="index">Record index for the shape</param>
        /// <returns>List of PolyCurve</returns>
        public IEnumerable<Curve> GetShapeAtRecord(int index)
        {
            return ShapeData[index].Select(pts => 
            {
                var points = pts.Select(p => Point.ByCoordinates(p.X, p.Y, p.Z));
                var polygon = NurbsCurve.ByPoints(points);
                foreach (var item in points)
                {
                    item.Dispose();
                }
                return polygon;
            });
        }

        /// <summary>
        /// Returns all shapes available in this shape file.
        /// </summary>
        /// <returns>List of PolyCurve</returns>
        public IEnumerable<Curve> GetAllShapes()
        {
            List<Curve> shapes = new List<Curve>();
            for (int i = 0; i < RecordCount; i++)
            {
                shapes.AddRange(GetShapeAtRecord(i));
            }

            return shapes;
        }

        /// <summary>
        /// Returns all points in the shape file.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<IEnumerable<Point>> GetAllPoints(int index)
        {
            return ShapeData[index].Select(pts => pts.Select(p => Point.ByCoordinates(p.X, p.Y, p.Z)));
        }
    }
}
