using System.Text.Json;
using System.Text.Json.Serialization;

namespace ArtModel.Core
{
    public enum GenerationCurve
    {
        Linear,
        // TODO: Добавить новые пресеты генерации
    }

    [Serializable()]
    public class ArtModelSerializer
    {
        public ArtUserInput UserInput { get; set; }

        public List<ArtGeneration> Generations { get; set; }

        public ArtModelSerializer()
        {
        }

        public ArtModelSerializer(ArtUserInput inputData)
        {
            UserInput = inputData;
            Generations = new List<ArtGeneration>();
            int generationsNumber = inputData.Generations;

            // Границы для деления значений
            double[] borders_normal = new double[generationsNumber];
            for (int gen = 0; gen < generationsNumber; gen++)
            {
                borders_normal[gen] = Math.Round((gen * 1.0) / (generationsNumber - 1), 3);
            }

            double[] borders_pairs = new double[generationsNumber + 1];
            for (int gen = 0; gen <= generationsNumber; gen++)
            {
                borders_pairs[gen] = Math.Round((gen * 1.0) / generationsNumber, 3);
            }

            // Генерация слоёв
            for (int gen = 0; gen < generationsNumber; gen++)
            {
                ArtGeneration artGeneration = new ArtGeneration();
                double factor_down = borders_pairs[gen];
                double factor_up = borders_pairs[gen + 1];
                double factor_norm = borders_normal[gen];

                // Ширина кисти
                int stroke_width_interval = inputData.StrokeWidth_Max - inputData.StrokeWidth_Min;
                artGeneration.StrokeWidth_Min = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_down);
                artGeneration.StrokeWidth_Max = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_up);

                // Длина мазка
                artGeneration.StrokeLength_Min = inputData.StrokeLength_Min;
                artGeneration.StrokeLength_Max = inputData.StrokeLength_Max;

                // Блюр
                int blur_interval = inputData.BlurSigma_Max - inputData.BlurSigma_Min;
                artGeneration.BlurSigma = (int)(inputData.BlurSigma_Min + blur_interval * factor_norm);

                // Дисперсия
                int dispersion_interval = inputData.Dispersion_Max - inputData.Dispersion_Min;
                artGeneration.DispersionBound = (int)(inputData.Dispersion_Min + dispersion_interval * factor_norm);

                // Итерации
                int iterations = (int)((1000 * 1000) / (Math.Pow(artGeneration.StrokeWidth_Max / 2, 2.5)));
                artGeneration.Iterations = iterations;

                Generations.Add(artGeneration);
            }

            Generations.Reverse();
        }
    }

    // Данные о конкретном уровне
    [Serializable()]
    public class ArtGeneration
    {
        public int Iterations { get; set; }

        public int StrokeWidth_Min { get; set; }
        public int StrokeWidth_Max { get; set; }

        public int StrokeLength_Min { get; set; }
        public int StrokeLength_Max { get; set; }

        public int BlurSigma { get; set; }
        public int DispersionBound { get; set; }
    }

    // Пользовательский ввод
    public class ArtUserInput
    {
        [JsonIgnore]
        public static readonly ArtUserInput Default = new ArtUserInput()
        {
            Generations = 7,
            Segments = 1,

            StrokeWidth_Min = 8,
            StrokeWidth_Max = 80,

            StrokeLength_Min = 1,
            StrokeLength_Max = 60,

            BlurSigma_Min = 8,
            BlurSigma_Max = 40,

            Dispersion_Min = 100,
            Dispersion_Max = 700,

            Curve = GenerationCurve.Linear,
        };

        // Количество поколений рисовки
        public int Generations { get; set; }

        // Сегментность мазков
        public int Segments { get; set; }

        // Минимальная и максимальная толщина кисти
        public int StrokeWidth_Min { get; set; }
        public int StrokeWidth_Max { get; set; }

        // Минимальная и максимальная длина мазка
        public int StrokeLength_Min { get; set; }
        public int StrokeLength_Max { get; set; }

        // Диапазон размытия изображений
        public int BlurSigma_Min { get; set; }
        public int BlurSigma_Max { get; init; }

        // Диапазон дисперсий для генерации
        public int Dispersion_Min { get; set; }
        public int Dispersion_Max { get; set; }

        // Способ разбиения на конкретные уровни
        public GenerationCurve Curve { get; set; }
    }
}
