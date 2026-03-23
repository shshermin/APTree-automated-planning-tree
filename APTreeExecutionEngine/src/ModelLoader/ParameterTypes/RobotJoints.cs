using System;
using System.Globalization;
using System.Linq;

namespace ModelLoader.ParameterTypes
{
    public class RobotJoints
    {
        public double[] Values { get; set; }

        public RobotJoints()
        {
            Values = Array.Empty<double>();
        }

        public RobotJoints(double[] values)
        {
            Values = values ?? Array.Empty<double>();
        }

        /// <summary>
        /// Parses a comma-separated string of doubles into a RobotJoints.
        /// </summary>
        public static RobotJoints Parse(string csv)
        {
            var parts = csv.Split(',');
            var values = parts.Select(p => double.Parse(p.Trim(), CultureInfo.InvariantCulture)).ToArray();
            return new RobotJoints(values);
        }

        public override string ToString()
        {
            return $"[{string.Join(", ", Values.Select(v => v.ToString("F6")))}]";
        }
    }
}
