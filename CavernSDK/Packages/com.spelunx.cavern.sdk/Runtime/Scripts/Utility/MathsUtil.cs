using System.Collections.Generic;
using UnityEngine;

namespace Spelunx
{
    /// <summary>
    /// Utility class for maths functions.
    /// </summary>
    public class MathsUtil
    {
        private MathsUtil() { }

        /// <summary>
        /// Solve and quadratic equation in the form ax^2 + bx + c = 0.
        /// </summary>
        /// <param name="a">Coefficient of x^2.</param>
        /// <param name="b">Coefficient of x.</param>
        /// <param name="c">Constant c.</param>
        /// <returns>
        /// Returns an empty list if the equation has no solutions.
        /// Returns a list with 1 element if the equation has 1 solution.
        /// Returns a list with 2 elements if the equation has 2 solution.
        /// </returns>
        public static List<float> SolveQuadraticEquation(float a, float b, float c)
        {
            List<float> solutions = new List<float>();

            // Case 1: The equation has no solution.
            float determinant = b * b - 4 * a * c;
            if (determinant < 0.0f) return solutions;

            // Case 2: The equation has 1 solution.
            float u = (-b - Mathf.Sqrt(b * b - 4.0f * a * c)) / (2.0f * a);
            if (determinant == 0.0f)
            {
                solutions.Add(u);
                return solutions;
            }

            // Case 3: The equation has 2 solutions.
            float v = (-b + Mathf.Sqrt(b * b - 4.0f * a * c)) / (2.0f * a);
            solutions.Add(Mathf.Min(u, v)); // Sort the solutions.
            solutions.Add(Mathf.Max(u, v));
            return solutions;
        }
    }
}