using ArtModel.Core;
using ArtModel.Tracing;
using MoreLinq;
using System.Drawing;

namespace ArtModel.Statistics
{
    public class TilesStatistics
    {
        public int TileWidth { get; set; }
        public int TileHeight { get; set; }
        public double AverageDispersion { get; set; }
        public double MedianDispersion { get; set; }
        public Dictionary<double, int> TilesDispersion { get; set; }

        public TilesStatistics()
        {
            TilesDispersion = new();
        }

        public void Sort()
        {
            TilesDispersion = TilesDispersion.OrderBy(obj => obj.Key).ToDictionary(obj => obj.Key, obj => obj.Value);
            AverageDispersion = GetAverageDispersion();
            MedianDispersion = GetMedianDispersion();
        }

        private double GetAverageDispersion()
        {
            double totalDispersion = 0;
            int totalValues = 0;
            foreach (var pair in TilesDispersion)
            {
                totalDispersion += pair.Key * pair.Value;
                totalValues += pair.Value;
            }
            return (int)(totalDispersion / totalValues);
        }

        private double GetMedianDispersion()
        {
            int totalValues = 0;
            foreach (var pair in TilesDispersion)
            {
                totalValues += pair.Value;
            }

            if (totalValues % 2 == 0)
            {
                int middleIndex = totalValues / 2;
                return (int)(TilesDispersion.ElementAt(middleIndex - 1).Key + TilesDispersion.ElementAt(middleIndex).Key) / 2.0;
            }
            else
            {
                int middleIndex = totalValues / 2;
                return (int)(TilesDispersion.ElementAt(middleIndex).Key);
            }
        }
    }

    public class StrokeStatistics
    {
        public class StrokeStatisticsData
        {
            public StrokePropertyCollection<double> StrokePropertyCollection { get; set; }
            public Color MeanColor { get; set; }
            public double Dispersion { get; set; }

            public StrokeStatisticsData(TracingResult tracingResult)
            {
                StrokePropertyCollection = tracingResult.SP;
                Dispersion = tracingResult.Dispersion;
                MeanColor = tracingResult.MeanColor;
            }
        }

        public ArtGeneration ArtGeneration { get; set; }

        public List<StrokeStatisticsData> StrokesData;

        public StrokeStatistics(ArtGeneration artGeneration)
        {
            StrokesData = new();
            ArtGeneration = artGeneration;
        }
    }

    public class ArtStatistics
    {
        public static ArtStatistics Instance { get; set; }

        public bool CollectStatistics { get; set; }

        public bool ShapesMap { get; set; }


        public Dictionary<int, TilesStatistics> TilesData;
        public Dictionary<int, StrokeStatistics> StrokesData;

        private int _currentGeneartion;
        private ArtGeneration _currentArtGen;

        public ArtStatistics()
        {
            TilesData = new();
            StrokesData = new();
        }

        public void AddStroke(TracingResult tracingResult)
        {
            if (StrokesData.ContainsKey(_currentGeneartion))
            {
                var strokeStatistics = StrokesData[_currentGeneartion];
                strokeStatistics.StrokesData.Add(new StrokeStatistics.StrokeStatisticsData(tracingResult));
            }
            else
            {
                var strokeStatistics = new StrokeStatistics(_currentArtGen);
                strokeStatistics.StrokesData.Add(new StrokeStatistics.StrokeStatisticsData(tracingResult));
                StrokesData.Add(_currentGeneartion, strokeStatistics);
            }
        }

        public void AddTileDispersion(double tileDispersion, int tileWidth, int tileHeight)
        {
            tileDispersion = (int)tileDispersion;

            if (TilesData.ContainsKey(_currentGeneartion))
            {
                var disp = TilesData[_currentGeneartion].TilesDispersion;
                if (disp.ContainsKey(tileDispersion))
                {
                    disp[tileDispersion]++;
                }
                else
                {
                    disp.Add(tileDispersion, 1);
                }
            }
            else
            {
                var stats = new TilesStatistics();
                stats.TileWidth = tileWidth;
                stats.TileHeight = tileHeight;
                stats.TilesDispersion.Add(tileDispersion, 1);
                TilesData.Add(_currentGeneartion, stats);
            }
        }

        public void SetGenerationContext(int generation, ArtGeneration artGeneration)
        {
            _currentGeneartion = generation;
            _currentArtGen = artGeneration;
        }
    }
}
