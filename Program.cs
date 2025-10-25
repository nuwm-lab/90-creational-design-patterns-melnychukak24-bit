using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;

namespace PolygonFactoryDemo
{
    /// <summary>
    /// Точка на площині (2D)
    /// </summary>
    public readonly record struct Point2D(double X, double Y)
    {
        public override string ToString() => $"({X:0.###}; {Y:0.###})";
    }

    /// <summary>
    /// Абстрактний клас багатокутника
    /// </summary>
    public abstract class Polygon
    {
        protected const double Tolerance = 1e-9;
        protected const double ToleranceSq = Tolerance * Tolerance;

        public ReadOnlyCollection<Point2D> Points { get; }

        protected Polygon(IEnumerable<Point2D> points)
        {
            var list = points.ToList();
            if (list.Count < 3)
                throw new ArgumentException("Багатокутник повинен мати щонайменше 3 вершини.");

            if (HasDuplicatePoints(list))
                throw new ArgumentException("Багатокутник містить повторювані точки.");

            Points = list.AsReadOnly();
        }

        /// <summary>
        /// Визначає, чи полігон опуклий.
        /// Якщо всі крос-продукти ≈ 0 (усі точки колінеарні), повертає false.
        /// </summary>
        protected static bool IsConvex(IList<Point2D> pts)
        {
            bool? sign = null;
            int n = pts.Count;

            for (int i = 0; i < n; i++)
            {
                var a = pts[i];
                var b = pts[(i + 1) % n];
                var c = pts[(i + 2) % n];

                double cross = CrossZ(a, b, c);
                if (Math.Abs(cross) < Tolerance) continue; // колінеарність

                bool currentSign = cross > 0;
                if (sign == null)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false;
            }

            // Якщо всі точки колінеарні або площа ≈ 0 — не вважаємо опуклим
            return Math.Abs(CalculateArea(pts)) > Tolerance;
        }

        /// <summary>
        /// Перевірка на дублікати точок із урахуванням допуску
        /// </summary>
        private static bool HasDuplicatePoints(IList<Point2D> points)
        {
            for (int i = 0; i < points.Count; i++)
            {
                for (int j = i + 1; j < points.Count; j++)
                {
                    double dx = points[i].X - points[j].X;
                    double dy = points[i].Y - points[j].Y;
                    if (dx * dx + dy * dy <= ToleranceSq)
                        return true;
                }
            }
            return false;
        }

        /// <summary>
        /// Обчислення орієнтованої площі (sign залежить від напрямку обходу)
        /// </summary>
        protected static double CalculateArea(IList<Point2D> pts)
        {
            double area = 0;
            for (int i = 0; i < pts.Count; i++)
            {
                var p1 = pts[i];
                var p2 = pts[(i + 1) % pts.Count];
                area += (p1.X * p2.Y - p2.X * p1.Y);
            }
            return area / 2.0;
        }

        /// <summary>
        /// Повертає абсолютну площу багатокутника
        /// </summary>
        public double Area() => Math.Abs(CalculateArea(Points));

        /// <summary>
        /// Z-компонента векторного добутку для трьох точок
        /// </summary>
        private static double CrossZ(Point2D a, Point2D b, Point2D c)
            => (b.X - a.X) * (c.Y - a.Y) - (b.Y - a.Y) * (c.X - a.X);

        /// <summary>
        /// Відстань між двома точками
        /// </summary>
        private static double Distance(Point2D a, Point2D b)
            => Math.Sqrt((b.X - a.X) * (b.X - a.X) + (b.Y - a.Y) * (b.Y - a.Y));

        /// <summary>
        /// Периметр багатокутника
        /// </summary>
        public double Perimeter()
        {
            double sum = 0;
            for (int i = 0; i < Points.Count; i++)
                sum += Distance(Points[i], Points[(i + 1) % Points.Count]);
            return sum;
        }

        public abstract string Type { get; }

        public override string ToString()
            => $"{Type} з {Points.Count} вершинами. " +
               $"Периметр = {Perimeter():0.###}, Площа = {Area():0.###}";
    }

    /// <summary>
    /// Опуклий багатокутник
    /// </summary>
    public sealed class ConvexPolygon : Polygon
    {
        public ConvexPolygon(IEnumerable<Point2D> points) : base(points)
        {
            if (!IsConvex(points.ToList()))
                throw new InvalidOperationException("Точки не утворюють опуклий багатокутник.");
        }

        public override string Type => "Опуклий багатокутник";
    }

    /// <summary>
    /// Неопуклий багатокутник
    /// </summary>
    public sealed class ConcavePolygon : Polygon
    {
        public ConcavePolygon(IEnumerable<Point2D> points) : base(points)
        {
            if (IsConvex(points.ToList()))
                throw new InvalidOperationException("Точки утворюють опуклий, а не неопуклий багатокутник.");
        }

        public override string Type => "Неопуклий багатокутник";
    }

    /// <summary>
    /// Абстрактна фабрика багатокутників
    /// </summary>
    public interface IPolygonFactory
    {
        Polygon CreatePolygon(IEnumerable<Point2D> points);
    }

    /// <summary>
    /// Фабрика опуклих багатокутників
    /// </summary>
    public sealed class ConvexPolygonFactory : IPolygonFactory
    {
        public Polygon CreatePolygon(IEnumerable<Point2D> points)
            => new ConvexPolygon(points);
    }

    /// <summary>
    /// Фабрика неопуклих багатокутників
    /// </summary>
    public sealed class ConcavePolygonFactory : IPolygonFactory
    {
        public Polygon CreatePolygon(IEnumerable<Point2D> points)
            => new ConcavePolygon(points);
    }

    internal class Program
    {
        static void Main()
        {
            try
            {
                IPolygonFactory convexFactory = new ConvexPolygonFactory();
                var convex = convexFactory.CreatePolygon(new[]
                {
                    new Point2D(0, 0),
                    new Point2D(4, 0),
                    new Point2D(4, 3),
                    new Point2D(0, 3)
                });
                Console.WriteLine(convex);

                IPolygonFactory concaveFactory = new ConcavePolygonFactory();
                var concave = concaveFactory.CreatePolygon(new[]
                {
                    new Point2D(0, 0),
                    new Point2D(4, 0),
                    new Point2D(2, 1),
                    new Point2D(4, 3),
                    new Point2D(0, 3)
                });
                Console.WriteLine(concave);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Помилка: {ex.Message}");
            }
        }
    }
}
