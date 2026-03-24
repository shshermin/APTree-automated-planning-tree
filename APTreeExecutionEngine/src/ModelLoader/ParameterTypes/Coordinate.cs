using System;
using System.Collections.Generic;
using System.Globalization;

namespace ModelLoader.ParameterTypes
{
    public class Coordinate
    {
        public double X { get; set; }
        public double Y { get; set; }
        public double Z { get; set; }

        public Coordinate()
        {
        }

        public Coordinate(double x, double y, double z)
        {
            X = x;
            Y = y;
            Z = z;
        }

        /// <summary>
        /// Parses a comma-separated string of exactly 3 values into a Coordinate.
        /// </summary>
        public static Coordinate Parse(string csv)
        {
            var parts = csv.Split(',');
            if (parts.Length != 3)
                throw new FormatException($"Coordinate expects 3 values, got {parts.Length}: '{csv}'");
            return new Coordinate(
                double.Parse(parts[0].Trim().Replace(" ", ""), CultureInfo.InvariantCulture),
                double.Parse(parts[1].Trim().Replace(" ", ""), CultureInfo.InvariantCulture),
                double.Parse(parts[2].Trim().Replace(" ", ""), CultureInfo.InvariantCulture)
            );
        }

        public override string ToString()
        {
            return $"({X}, {Y}, {Z})";
        }
    }
}
