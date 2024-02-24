using ArtModel.Core;
using ArtModel.Tracing;

namespace ArtModel.StrokeLib
{
    public class StrokeLibrary
    {
        private double mm_tp_px_coef = 8.6;
        static string sourceLibraryPath = "..\\..\\..\\..\\ArtModel\\StrokeLib\\SourceLib";

        private Dictionary<int, List<Stroke>> _strokesData;

        static StrokeLibrary()
        {

        }

        public StrokeLibrary(double resizeCoefficient = 1)
        {
            mm_tp_px_coef *= resizeCoefficient;
            _strokesData = StrokeLibraryReader.ReadAllStrokes(sourceLibraryPath, resizeCoefficient);
        }

        public Stroke ClassifyStroke(TracingResult targetStroke, ArtGeneration genData)
        {
            double points = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Points);

            switch (points)
            {
                case 1:
                    return ClassifyPt1(targetStroke).Copy();
                case 2:
                    return ClassifyPt2(targetStroke).Copy();
                case 3:
                    return ClassifyPt3(targetStroke).Copy();
                default:
                    goto case 1;
            }
        }

        private Stroke ClassifyPt1(TracingResult targetStroke)
        {
            double width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);

            return _strokesData[1]
                .OrderBy(sourceStroke => Math.Abs(sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef - width))
                .First();
        }

        private Stroke ClassifyPt2(TracingResult targetStroke)
        {
            double target_width = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
            double target_length = targetStroke.StrokeProperties.GetProperty(StrokeProperty.Length);
            double div = target_length / target_width;
            double real_width_weight = 0;

            return _strokesData[2]
               .OrderBy(sourceStroke =>
                   {
                       double source_width = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Width);
                       double source_length = sourceStroke.StrokeProperties.GetProperty(StrokeProperty.Length);

                       return Math.Abs(source_length / source_width - div) /*+real_width_weight * Math.Abs(target_width - source_width)*/;
                   })
               .First();
        }

        private Stroke ClassifyPt3(TracingResult targetStroke)
        {
            return _strokesData[3][0];
        }

        public double CalculateResizeCoefficient(TracingResult tracingResult, Stroke strokeData)
        {
            double points = tracingResult.StrokeProperties.GetProperty(StrokeProperty.Points);
            switch (points)
            {
                case 1:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                case 2:
                    return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Width) / (strokeData.StrokeProperties.GetProperty(StrokeProperty.Width) * mm_tp_px_coef));
                   // return (tracingResult.StrokeProperties.GetProperty(StrokeProperty.Length) / strokeData.Height);
                default:
                    return (0.0);
            }
        }

    }
}
