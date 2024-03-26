using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing
{
    public enum StrokeProperty
    {
        Points, // Количество точек мазка

        Width, // Ширина мазка
        Length, // Суммарная длина мазка по сегментам
        LtoW, // Отношение W к L

        Angle1, // Углы между сегментами
        Fraction // Доли сегментов от общей длины
    }

    public class StrokePropertyCollection<T> : Dictionary<StrokeProperty, T>
    {
        private object locker;

        public StrokePropertyCollection()
        {
            locker = new object();
        }

        public virtual bool CheckPropery(StrokeProperty key)
        {
            return ContainsKey(key);
        }

        public virtual T GetP(StrokeProperty key)
        {
            return this[key];
        }

        public virtual void SetP(StrokeProperty key, T value)
        {
            if (ContainsKey(key))
            {
                this[key] = value;
            }
            else
            {
                lock (locker)
                {
                    TryAdd(key, value);
                }
            }
        }

        public static StrokeProperty StrokePropertyByAlias(string key)
        {
            return StrokePropertyAliases[key];
        }

        private static Dictionary<string, StrokeProperty> StrokePropertyAliases = new()
        {
            { "pt" , StrokeProperty.Points },
            { "w" , StrokeProperty.Width },
            { "l" , StrokeProperty.Length },
            { "a" , StrokeProperty.Angle1 },
            { "s" , StrokeProperty.Fraction },
        };

        public override bool Equals(object? obj)
        {
            return base.Equals(obj);
        }

        public override int GetHashCode()
        {
            return base.GetHashCode();
        }
    }
}
