using System;
using System.Collections.Generic;
using System.Linq;

namespace PolygonFactoryExample
{
    /// <summary>
    /// Представляє двовимірну точку.
    /// </summary>
    public struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y)
        {
            X = x;
            Y = y;
        }

        public static Point2D operator -(Point2D a, Point2D b)
            => new Point2D(a.X - b.X, a.Y - b.Y);

        public override string ToString() => $"({X:F2}, {Y:F2})";
    }

    /// <summary>
    /// Клас для представлення простого (несамоперетинного) багатокутника.
    /// </summary>
    public class Polygon
    {
        private const double Epsilon = 1e-12;

        public IReadOnlyList<Point2D> Points { get; }

        public Polygon(IEnumerable<Point2D> points)
        {
            var list = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
            if (list.Count < 3)
                throw new ArgumentException("Багатокутник повинен мати щонайменше 3 вершини.");
            Points = list;
        }

        /// <summary>Обчислює площу (за формулою Гауса).</summary>
        public double Area()
        {
            double sum = 0;
            for (int i = 0; i < Points.Count; i++)
            {
                var a = Points[i];
                var b = Points[(i + 1) % Points.Count];
                sum += a.X * b.Y - a.Y * b.X;
            }
            return Math.Abs(sum) * 0.5;
        }

        /// <summary>Обчислює периметр багатокутника.</summary>
        public double Perimeter()
        {
            double sum = 0;
            for (int i = 0; i < Points.Count; i++)
            {
                var a = Points[i];
                var b = Points[(i + 1) % Points.Count];
                double dx = a.X - b.X;
                double dy = a.Y - b.Y;
                sum += Math.Sqrt(dx * dx + dy * dy);
            }
            return sum;
        }

        /// <summary>
        /// Перевіряє, чи є багатокутник опуклим.
        /// Ігнорує колінеарні вершини.
        /// </summary>
        public bool IsConvex()
        {
            int n = Points.Count;
            if (n < 3) return false;

            int sign = 0;

            for (int i = 0; i < n; i++)
            {
                var a = Points[i];
                var b = Points[(i + 1) % n];
                var c = Points[(i + 2) % n];

                var ab = b - a;
                var bc = c - b;

                double crossZ = ab.X * bc.Y - ab.Y * bc.X;

                if (Math.Abs(crossZ) <= Epsilon)
                    continue;

                int currentSign = crossZ > 0 ? 1 : -1;
                if (sign == 0)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false;
            }

            return true;
        }

        /// <summary>
        /// Перевірка на самоперетин.
        /// </summary>
        public bool HasSelfIntersections()
        {
            int n = Points.Count;
            for (int i = 0; i < n; i++)
            {
                var a1 = Points[i];
                var a2 = Points[(i + 1) % n];

                for (int j = i + 1; j < n; j++)
                {
                    if (Math.Abs(i - j) <= 1 || (i == 0 && j == n - 1))
                        continue;

                    var b1 = Points[j];
                    var b2 = Points[(j + 1) % n];

                    if (SegmentsIntersect(a1, a2, b1, b2))
                        return true;
                }
            }
            return false;
        }

        private static bool SegmentsIntersect(Point2D p1, Point2D p2, Point2D p3, Point2D p4)
        {
            static double Cross(Point2D a, Point2D b, Point2D c)
                => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

            double d1 = Cross(p1, p2, p3);
            double d2 = Cross(p1, p2, p4);
            double d3 = Cross(p3, p4, p1);
            double d4 = Cross(p3, p4, p2);

            return ((d1 * d2 < 0) && (d3 * d4 < 0));
        }

        public override string ToString()
            => $"Багатокутник із {Points.Count} вершинами, Площа={Area():F3}, Периметр={Perimeter():F3}";
    }

    /// <summary>Інтерфейс абстрактної фабрики для створення багатокутників.</summary>
    public interface IPolygonFactory
    {
        Polygon Create(IList<Point2D> points);
    }

    /// <summary>Фабрика для створення опуклих багатокутників.</summary>
    public class ConvexPolygonFactory : IPolygonFactory
    {
        public Polygon Create(IList<Point2D> points)
        {
            var poly = new Polygon(points);
            if (poly.HasSelfIntersections())
                throw new ArgumentException("Багатокутник має самоперетини.");
            if (!poly.IsConvex())
                throw new ArgumentException("Передані точки не утворюють опуклий багатокутник.");
            return poly;
        }
    }

    /// <summary>Фабрика для створення неопуклих багатокутників.</summary>
    public class ConcavePolygonFactory : IPolygonFactory
    {
        public Polygon Create(IList<Point2D> points)
        {
            var poly = new Polygon(points);
            if (poly.HasSelfIntersections())
                throw new ArgumentException("Багатокутник має самоперетини.");
            if (poly.IsConvex())
                throw new ArgumentException("Передані точки утворюють опуклий багатокутник, очікувався неопуклий.");
            return poly;
        }
    }

    /// <summary>Продюсер фабрик для вибору типу багатокутника.</summary>
    public static class PolygonFactoryProducer
    {
        public static IPolygonFactory GetFactory(bool convex)
        {
            return convex ? new ConvexPolygonFactory() as IPolygonFactory
                          : new ConcavePolygonFactory();
        }
    }

    internal class Program
    {
        private static int Main(string[] args)
        {
            try
            {
                // Опуклий багатокутник — квадрат
                var convexPoints = new List<Point2D>
                {
                    new Point2D(0,0),
                    new Point2D(2,0),
                    new Point2D(2,2),
                    new Point2D(0,2)
                };

                // Неопуклий багатокутник — "стрілка"
                var concavePoints = new List<Point2D>
                {
                    new Point2D(0,0),
                    new Point2D(2,0),
                    new Point2D(1,1),
                    new Point2D(2,2),
                    new Point2D(0,2)
                };

                var convexFactory = PolygonFactoryProducer.GetFactory(convex: true);
                var concaveFactory = PolygonFactoryProducer.GetFactory(convex: false);

                var convexPoly = convexFactory.Create(convexPoints);
                Console.WriteLine("✅ Опуклий багатокутник створено:");
                Console.WriteLine(convexPoly);

                var concavePoly = concaveFactory.Create(concavePoints);
                Console.WriteLine("\n✅ Неопуклий багатокутник створено:");
                Console.WriteLine(concavePoly);

                // Приклад помилки — передано опуклі точки у фабрику неопуклих
                try
                {
                    concaveFactory.Create(convexPoints);
                }
                catch (ArgumentException ex)
                {
                    Console.WriteLine($"\n⚠ Очікувана помилка: {ex.Message}");
                }

                return 0;
            }
            catch (Exception ex)
            {
                Console.Error.WriteLine($"❌ Помилка: {ex.Message}");
                return 1;
            }
        }
    }
}
