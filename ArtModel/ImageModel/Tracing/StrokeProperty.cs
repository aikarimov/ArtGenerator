using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.ImageModel.Tracing
{
    public enum StrokeProperty
    {
        Points, // Количество точек мазка
        Width, // Ширина мазка
        Length, // Суммарная длина мазка по сегментам
        Angle, // Углы между сегментами
        Fraction // Доли сегментов от общей длины
    }

    public class StrokePropertyCollection : Dictionary<StrokeProperty, double>
    {
        public StrokePropertyCollection()
        {

        }

        // TODO: Добавить поддержку списка свойств
        public double GetProperty(StrokeProperty key)
        {
            return this[key];
        }

        public void SetProperty(StrokeProperty key, double value)
        {
            if (ContainsKey(key))
            {
                this[key] = value;
            }
            else
            {
                Add(key, value);
            }
        }

        public void SetProperty(string key, double value) => SetProperty(StrokePropertyAliases[key], value);

        private static Dictionary<string, StrokeProperty> StrokePropertyAliases = new()
        {
            { "pt" , StrokeProperty.Points },
            { "w" , StrokeProperty.Width },
            { "l" , StrokeProperty.Length },
            { "a" , StrokeProperty.Angle },
            { "s" , StrokeProperty.Fraction },
        };
    }
}
