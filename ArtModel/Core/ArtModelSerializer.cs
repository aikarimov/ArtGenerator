using System.Text.Json.Serialization;

namespace ArtModel.Core
{
    public enum GenerationCurve
    {
        Linear,
        // TODO: Добавить новые пресеты генерации
        // Лень
    }

    public class ArtModelSerializer
    {
        public ArtUserInput UserInput { get; set; }

        [JsonIgnore]
        public int Width { get; set; }

        [JsonIgnore]
        public int Height { get; set; }

        public List<ArtGeneration> Generations { get; set; }

        public ArtModelSerializer()
        {
        }

        public ArtModelSerializer(ArtUserInput inputData, int width, int height)
        {
            Width = width;
            Height = height;
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
                ArtGeneration aGen = new ArtGeneration();
                double factor_down = borders_pairs[gen];
                double factor_up = borders_pairs[gen + 1];
                double factor_norm = borders_normal[gen];

                // Ширина кисти
                int stroke_width_interval = inputData.StrokeWidth_Max - inputData.StrokeWidth_Min;
                aGen.StrokeWidth_Min = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_down);
                aGen.StrokeWidth_Max = (int)(inputData.StrokeWidth_Min + stroke_width_interval * factor_up);

                // Длина мазка
                aGen.StrokeLength_Min = inputData.StrokeLength_Min;
                aGen.StrokeLength_Max = inputData.StrokeLength_Max;

                // Блюр
                int blur_interval = inputData.BlurSigma_Max - inputData.BlurSigma_Min;
                aGen.BlurSigma = (int)(inputData.BlurSigma_Min + blur_interval * factor_norm);

                // Дисперсия
                int dispersion_stroke_interval = inputData.Dispersion_Stroke_Max - inputData.Dispersion_Stroke_Min;
                aGen.DispersionStrokeBound = (int)(inputData.Dispersion_Stroke_Min + dispersion_stroke_interval * factor_norm);

                int dispersion_tile_interval = inputData.Dispersion_Tile_Max - inputData.Dispersion_Tile_Min;
                aGen.DispersionTileBound = (int)(inputData.Dispersion_Tile_Min + dispersion_tile_interval * factor_norm);

                // Итерации
                // Можно смело изменять формулу
                int iterations = (int)((width * height) / (aGen.StrokeWidth_Max * aGen.StrokeWidth_Max));
                aGen.Iterations = iterations;

                Generations.Add(aGen);
            }

            Generations.Reverse();
        }
    }

    // Данные о конкретном уровне
    public class ArtGeneration
    {
        public int Iterations { get; set; }

        public int StrokeWidth_Min { get; set; }
        public int StrokeWidth_Max { get; set; }

        public int StrokeLength_Min { get; set; }
        public int StrokeLength_Max { get; set; }

        public int BlurSigma { get; set; }
        public int DispersionStrokeBound { get; set; }
        public int DispersionTileBound { get; set; }
    }

    // Пользовательский ввод
    public class ArtUserInput
    {
        [JsonIgnore]
        public static readonly ArtUserInput Default = new ArtUserInput()
        {
            Generations = 7,
            Segments = 2,

            StrokeWidth_Min = 8,
            StrokeWidth_Max = 80,

            StrokeLength_Min = 3,
            StrokeLength_Max = 150,

            BlurSigma_Min = 8,
            BlurSigma_Max = 40,

            Dispersion_Stroke_Min = 100,
            Dispersion_Stroke_Max = 700,

            Dispersion_Tile_Min = 5000,
            Dispersion_Tile_Max = 20000,

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
        public int Dispersion_Stroke_Min { get; set; }
        public int Dispersion_Stroke_Max { get; set; }

        // Диапазон дисперсий для регионов
        public int Dispersion_Tile_Min { get; set; }
        public int Dispersion_Tile_Max { get; set; }

        // Способ разбиения на конкретные уровни
        public GenerationCurve Curve { get; set; }
    }
}
