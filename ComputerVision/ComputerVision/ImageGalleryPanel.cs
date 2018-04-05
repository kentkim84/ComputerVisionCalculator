using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace ComputerVision
{
    public class ImageGalleryPanel : Panel
    {
        public static readonly DependencyProperty ItemsInFirstRowProperty = DependencyProperty.Register(
            "ItemsInFirstRow", typeof(int), typeof(ImageGalleryPanel), new PropertyMetadata(default(int)));

        public static readonly DependencyProperty ItemsInSecondRowProperty = DependencyProperty.Register(
            "ItemsInSecondRow", typeof(int), typeof(ImageGalleryPanel), new PropertyMetadata(default(int)));

        public int ItemsInFirstRow
        {
            get => (int)GetValue(ItemsInFirstRowProperty);
            set => SetValue(ItemsInFirstRowProperty, value);
        }
        public int ItemsInSecondRow
        {
            get => (int)GetValue(ItemsInSecondRowProperty);
            set => SetValue(ItemsInSecondRowProperty, value);
        }

        #region Size Measure

        protected override Size MeasureOverride(Size availableSize)
        {
            if (IsEmpty() || ItemsInFirstRow == 0 || ItemsInSecondRow == 0)
                return new Size(0, 0);

            double firstRowItemWidth = availableSize.Width / ItemsInFirstRow;
            double secondRowItemWidth = availableSize.Width / ItemsInSecondRow;

            int count = GetCount();
            for (var i = 0; i < count; i++)
            {
                Size childAvailableSize = i + 1 <= ItemsInFirstRow ?
                    new Size(firstRowItemWidth, availableSize.Height) :
                    new Size(secondRowItemWidth, availableSize.Height);

                Children[i].Measure(childAvailableSize);
            }

            double childrenDesiredHeight = Children.Count <= ItemsInFirstRow ?
                Children[0].DesiredSize.Height : Children[0].DesiredSize.Height + Children[count - 1].DesiredSize.Height;

            double height = Math.Min(availableSize.Height, childrenDesiredHeight);

            return new Size(availableSize.Width, height);
        }

        private bool IsEmpty() => Children == null || Children.Count == 0;

        private int GetCount() => Math.Min(ItemsInFirstRow + ItemsInSecondRow, Children.Count);

        protected override Size ArrangeOverride(Size finalSize)
        {
            if (IsEmpty())
                return new Size(0, 0);

            double x = .0;
            double y = .0;

            int count = GetCount();
            for (int i = 0; i < count; i++)
            {
                Children[i].Arrange(new Rect(new Point(x, y), Children[i].DesiredSize));

                x += Children[i].DesiredSize.Width;
                if (i + 1 == ItemsInFirstRow)
                {
                    x = .0;
                    y = Children[0].DesiredSize.Height;
                }
            }

            return finalSize;
        }

        #endregion Size Measure

    }
}
