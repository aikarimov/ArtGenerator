using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ArtModel.Tracing.PointDeciding
{
    public interface IPointDecider
    {
        // Получение новой точки
        public (int x, int y) GetNewPoint();

        // Доступны ли новые точки
        public bool DeciderAvaliable();

        // Обратная связь после выбора точки
        public void PointCallback((int x, int y) point);

        // Обновить состояне после всего мазка
        public void PostStroke();
    }
}
