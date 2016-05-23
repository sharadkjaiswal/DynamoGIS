using System.Collections.Generic;
using System.Linq;
using Autodesk.DesignScript.Geometry;
using EGIS.ShapeFileLib;
using System.IO;

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
        /// Represents the attributes Database
        /// </summary>
        private DbfReader Database { get; set; }

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
                for (int i = 0; i < RecordCount; ++i)
                {
                    var shapeData = file.GetShapeDataD(i);
                    ShapeData[i] = shapeData.Select(x => x.Select(p => new PointXYZ() { X = p.X, Y = p.Y, Z = 0 }).ToArray());
                }
            }
            string dbfFilePath = Path.ChangeExtension(shapeFilePath, "dbf");
            Database = new DbfReader(dbfFilePath);
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
                var points = pts.Select(p => Point.ByCoordinates(p.X, p.Y, p.Z)).ToList();

                var pts0 = points.Where((x, i) => i == 0 || !(x.IsAlmostEqualTo(points[i - 1]))).ToList();

                var polygon = PolyCurve.ByPoints(pts0);
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
        /// Returns all shapes in accordance to the records available in this shape file.
        /// </summary>
        /// <returns>List of PolyCurve</returns>
        public IEnumerable<IEnumerable<Curve>> GetAllShapesInAllRecords()
        {
            var shapes = new List<IEnumerable<Curve>>();
            for (int i = 0; i < RecordCount; i++)
            {
                shapes.Add(GetShapeAtRecord(i));
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

        /// <summary>
        /// Returns all the field names of the Database.
        /// </summary>
        /// <returns></returns>
        public IEnumerable<string> GetFieldNames()
        {
            return Database.GetFieldNames();
        }

        /// <summary>
        /// Gets the field values at a given record index.
        /// </summary>
        /// <param name="index">Record index for the shape</param>
        /// <returns>List of string</returns>
        public IEnumerable<string> GetFieldsAtRecord(int index)
        {
            return Database.GetFields(index);
        }

        /// <summary>
        /// Returns all the field values from the DBF object.
        /// </summary>
        /// <returns>List of string</returns>
        public IEnumerable<string> GetAllFields()
        {
            var fields = new List<string>();
            for (int i = 0; i < RecordCount; i++)
            {
                fields.AddRange(GetFieldsAtRecord(i));
            }

            return fields;
        }

        /// <summary>
        /// Gets all the records field values at a given field index.
        /// </summary>
        /// <param name="index">field index</param>
        /// <returns>List of string</returns>
        public IEnumerable<string> GetRecordsAtField(int index)
        {
            return Database.GetRecords(index);
        }

        /// <summary>
        /// Gets all the records field values at a given field name.
        /// </summary>
        /// <param name="fieldName">field name</param>
        /// <returns>List of string</returns>
        public IEnumerable<string> GetRecordsAtFieldName(string fieldName)
        {
            int index = Database.IndexOfFieldName(fieldName);
            if (index > -1)
            {
                return Database.GetRecords(index);
            }
            return null;
        }

        /// <summary>
        /// Gets the field value at given record and field indexes.
        /// </summary>
        /// <param name="recordIndex">record index</param>
        /// /// <param name="fieldIndex">field index</param>
        /// <returns>String</returns>
        public string GetFieldValue(int recordIndex, int fieldIndex)
        {
            return Database.GetField(recordIndex, fieldIndex);
        }

        /// <summary>
        /// Gets the index of the given field name.
        /// </summary>
        /// <param name="fieldName">field name</param>
        /// <returns>Integer, -1 if field name cannot be found</returns>
        public int GetIndexOfField(string fieldName)
        {
            return Database.IndexOfFieldName(fieldName);
        }

    }
}
