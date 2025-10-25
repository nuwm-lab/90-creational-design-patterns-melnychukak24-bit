using System;
using System.Collections.Generic;
using System.Linq;

namespace PolygonFactoryExample
{
    // Проста структура точки з double координатами
    public struct Point2D
    {
        public double X { get; }
        public double Y { get; }

        public Point2D(double x, double y) { X = x; Y = y; }

        public static Point2D operator -(Point2D a, Point2D b) => new Point2D(a.X - b.X, a.Y - b.Y);
    }

    // Базовий клас багатокутника
    public class Polygon
    {
        public IReadOnlyList<Point2D> Points { get; }

        public Polygon(IEnumerable<Point2D> points)
        {
            var list = points?.ToList() ?? throw new ArgumentNullException(nameof(points));
            if (list.Count < 3) throw new ArgumentException("Polygon must have at least 3 points.");
            Points = list;
        }

        // Площа (формула Гауса, абсолютна)
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

        // Периметр
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

        // Статичний метод перевірки опуклості (припускає простий (non-self-intersecting) поліг.)
        public bool IsConvex()
        {
            int n = Points.Count;
            if (n < 3) return false;

            // Знаходимо знак перших ненульових векторних добутків
            int sign = 0;

            for (int i = 0; i < n; i++)
            {
                var a = Points[i];
                var b = Points[(i + 1) % n];
                var c = Points[(i + 2) % n];

                var ab = b - a;
                var bc = c - b;

                double crossZ = ab.X * bc.Y - ab.Y * bc.X; // z-компонента векторного добутку 2D

                if (Math.Abs(crossZ) <= 1e-12) // колінеарні точки ігноруємо
                    continue;

                int currentSign = crossZ > 0 ? 1 : -1;

                if (sign == 0)
                    sign = currentSign;
                else if (sign != currentSign)
                    return false; // зміна знаку -> не опуклий
            }

            // Якщо всі три послідовні точки колінеарні — вважати не опуклим
            return sign != 0;
        }
    }

    // Інтерфейс абстрактної фабрики
    public interface IPolygonFactory
    {
        /// <summary>
        /// Створює багатокутник з наданих точок. Якщо точки не відповідають вимозі (опуклий/неопуклий),
        /// фабрика повинна кинути ArgumentException або спробувати скорегувати (у цій реалізації кинемо).
        /// </summary>
        Polygon Create(IList<Point2D> points);
    }

    // Фабрика для опуклих багатокутників
    public class ConvexPolygonFactory : IPolygonFactory
    {
        public Polygon Create(IList<Point2D> points)
        {
            var poly = new Polygon(points);
            if (!poly.IsConvex())
                throw new ArgumentException("Provided points do not form a convex polygon.");
            return poly;
        }
    }

    // Фабрика для неопуклих (concave) багатокутників
    public class ConcavePolygonFactory : IPolygonFactory
    {
        public Polygon Create(IList<Point2D> points)
        {
            var poly = new Polygon(points);
            if (poly.IsConvex())
                throw new ArgumentException("Provided points form a convex polygon; expected concave.");
            return poly;
        }
    }

    // Optional: фабричний продюсер для вибору фабрики за enum / прапорцем
    public static class PolygonFactoryProducer
    {
        public static IPolygonFactory GetFactory(bool convex)
        {
            return convex ? new ConvexPolygonFactory() as IPolygonFactory : new ConcavePolygonFactory();
        }
    }

    // Демонстрація
    class Program
    {
        static void Main()
        {
            // Опуклий: простий квадрат
            var convexPoints = new List<Point2D>
            {
                new Point2D(0,0),
                new Point2D(1,0),
                new Point2D(1,1),
                new Point2D(0,1)
            };

            // Неопуклий: "стрілка"
            var concavePoints = new List<Point2D>
            {
                new Point2D(0,0),
                new Point2D(2,0),
                new Point2D(1,1),
                new Point2D(2,2),
                new Point2D(0,2)
            };

            IPolygonFactory convexFactory = PolygonFactoryProducer.GetFactory(convex: true);
            IPolygonFactory concaveFactory = PolygonFactoryProducer.GetFactory(convex: false);

            try
            {
                var convexPoly = convexFactory.Create(convexPoints);
                Console.WriteLine("Convex polygon created:");
                Console.WriteLine($"Area = {convexPoly.Area():F3}, Perimeter = {convexPoly.Perimeter():F3}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Convex factory error: {ex.Message}");
            }

            try
            {
                var concavePoly = concaveFactory.Create(concavePoints);
                Console.WriteLine("Concave polygon created:");
                Console.WriteLine($"Area = {concavePoly.Area():F3}, Perimeter = {concavePoly.Perimeter():F3}");
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Concave factory error: {ex.Message}");
            }

            // Приклад помилки: передамо опуклі точки до concaveFactory
            try
            {
                var wrong = concaveFactory.Create(convexPoints); // кинеться
            }
            catch (ArgumentException ex)
            {
                Console.WriteLine($"Expected error (convex->concave): {ex.Message}");
            }
        }
    }
}
