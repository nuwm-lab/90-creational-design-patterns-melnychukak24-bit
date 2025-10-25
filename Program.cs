using System;
using System.Collections.Generic;
using System.Linq;

namespace PolygonFactoryExample
{
    /// <summary>
    /// Представляє двовимірну точку (немутуючий тип).
    /// </summary>
    public readonly struct Point2D
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

        public override bool Equals(object? obj)
            => obj is Point2D p && Math.Abs(X - p.X) < 1e-9 && Math.Abs(Y - p.Y) < 1e-9;

        public override int GetHashCode() => HashCode.Combine(X, Y);
    }

    /// <summary>
    /// Клас для представлення простого (несамоперетинного) багатокутника.
    /// </summary>
    public class Polygon
    {
        private const double Tolerance = 1e-9;

        public IReadOnlyList<Point2D> Points { get; }

        public Polygon(IEnumerable<Point2D> points)
        {
            var list = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
            if (list.Count < 3)
                throw new ArgumentException("Багатокутник повинен мати щонайменше 3 вершини.");

            if (HasDuplicatePoints(list))
                throw new ArgumentException("Багатокутник має повторювані вершини.");

            Points = list;
        }

        private static bool HasDuplicatePoints(IList<Point2D> points)
        {
            var set = new HashSet<Point2D>();
            foreach (var p in points)
            {
                if (!set.Add(p)) return true;
            }
            return false;
        }

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

        /// <summary>Перевіряє, чи багатокутник опуклий (ігнорує колінеарні точки).</summary>
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

                if (Math.Abs(crossZ) <= Tolerance)
                    continue;

                int currentSign = crossZ > 0 ? 1 : -1;
                if (sign == 0)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false;
            }

            return true;
        }

        /// <summary>Перевірка на самоперетини, включно з колінеарними відрізками.</summary>
        public bool HasSelfIntersections()
        {
            int n = Points.Count;
            for (int i = 0; i < n; i++)
            {
                var a1 = Points[i];
                var a2 = Points[(i + 1) % n];

                for (int j = i + 1; j < n; j++)
                {
                    // Пропускаємо суміжні ребра
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
            static int Orientation(Point2D a, Point2D b, Point2D c)
            {
                double val = (b.Y - a.Y) * (c.X - b.X) - (b.X - a.X) * (c.Y - b.Y);
                if (Math.Abs(val) < Tolerance) return 0; // колінеарні
                return (val > 0) ? 1 : 2; // 1 = CW, 2 = CCW
            }

            static bool OnSegment(Point2D a, Point2D b, Point2D c)
            {
                return c.X <= Math.Max(a.X, b.X) + Tolerance && c.X >= Math.Min(a.X, b.X) - Tolerance &&
                       c.Y <= Math.Max(a.Y, b.Y) + Tolerance && c.Y >= Math.Min(a.Y, b.Y) - Tolerance;
            }

            int o1 = Orientation(p1, p2, p3);
            int o2 = Orientation(p1, p2, p4);
            int o3 = Orientation(p3, p4, p1);
            int o4 = Orientation(p3, p4, p2);

            if (o1 != o2 && o3 != o4)
                return true;

            // спеціальні випадки — колінеарні точки
            if (o1 == 0 && OnSegment(p1, p2, p3)) return true;
            if (o2 == 0 && OnSegment(p1, p2, p4)) return true;
            if (o3 == 0 && OnSegment(p3, p4, p1)) return true;
            if (o4 == 0 && OnSegment(p3, p4, p2)) return true;

            return false;
        }

        public override string ToString()
            => $"Багатокутник із {Points.Count} вершин, Площа={Area():F3}, Периметр={Perimeter():F3}";
    }

    /// <summary>Тип багатокутника для вибору фабрики.</summary>
    public enum PolygonKind
    {
        Convex,
        Concave
    }

    public interface IPolygonFactory
    {
        Polygon Create(IEnumerable<Point2D> points);
    }

    public class ConvexPolygonFactory : IPolygonFactory
    {
        public Polygon Create(IEnumerable<Point2D> points)
        {
            var poly = new Polygon(points);
            if (poly.HasSelfIntersections())
                throw new InvalidOperationException("Багатокутник має самоперетини.");
            if (!poly.IsConvex())
                throw new InvalidOperationException("Передані точки не утворюють опуклий багатокутник.");
            return poly;
        }
    }

    public class ConcavePolygonFactory : IPolygonFactory
    {
        public Polygon Create(IEnumerable<Point2D> points)
        {
            var poly = new Polygon(points);
            if (poly.HasSelfIntersections())
                throw new InvalidOperationException("Багатокутник має самоперетини.");
            if (poly.IsConvex())
                throw new InvalidOperationException("Передані точки утворюють опуклий багатокутник, очікувався неопуклий.");
            return poly;
        }
    }

    public static class PolygonFactoryProducer
    {
        private static readonly IPolygonFactory convexFactory = new ConvexPolygonFactory();
        private static readonly IPolygonFactory concaveFactory = new ConcavePolygonFactory();

        public static IPolygonFactory GetFactory(PolygonKind kind)
            => kind == PolygonKind.Convex ? convexFactory : concaveFactory;
    }

    public class Program
    {
        public static int Main(string[] args)
        {
            try
            {
                var convexPoints = new[]
                {
                    new Point2D(0,0),
                    new Point2D(2,0),
                    new Point2D(2,2),
                    new Point2D(0,2)
                };

                var concavePoints = new[]
                {
                    new Point2D(0,0),
                    new Point2D(2,0),
                    new Point2D(1,1),
                    new Point2D(2,2),
                    new Point2D(0,2)
                };

                var convexPoly = PolygonFactoryProducer.GetFactory(PolygonKind.Convex).Create(convexPoints);
                Console.WriteLine("✅ Опуклий багатокутник створено:");
                Console.WriteLine(convexPoly);

                var concavePoly = PolygonFactoryProducer.GetFactory(PolygonKind.Concave).Create(concavePoints);
                Console.WriteLine("\n✅ Неопуклий багатокутник створено:");
                Console.WriteLine(concavePoly);

                try
                {
                    PolygonFactoryProducer.GetFactory(PolygonKind.Concave).Create(convexPoints);
                }
                catch (Exception ex)
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
