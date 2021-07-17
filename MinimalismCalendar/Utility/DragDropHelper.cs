using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Windows.Foundation;
using Windows.UI.Xaml;
using Windows.UI.Xaml.Controls;

namespace MinimalismCalendar.Utility
{
    /// <summary>
    /// Contains methods for supporting drag and drop.
    /// </summary>
    public static class DragDropHelper
    {
        /// <summary>
        /// Gets the insertion index into the target list view from the point the drop occured over the target list view.
        /// </summary>
        /// <param name="e">The event args from the drop event.</param>
        /// <param name="targetListView">The target list view the data was dropped on.</param>
        /// <returns></returns>
        public static int GetDropInsertionIndex(DragEventArgs e, ListView targetListView)
        {
            // Handle the case where there are no items currently in the target list.
            if (targetListView.Items.Count == 0)
            {
                return 0;
            }

            // Get the position of the drop relative to the list view's items panel.
            Point position = e.GetPosition(targetListView.ItemsPanelRoot);

            // Find the height of the first item in the target list view.
            // Get a reference to the first item's container.
            ListViewItem sampleItem = targetListView.ContainerFromIndex(0) as ListViewItem;

            // Adjust the item height for margins.
            double itemHeight = sampleItem.ActualHeight + sampleItem.Margin.Top + sampleItem.Margin.Bottom;

            // Find the index by dividing the number of items by the height of each item.
            int index = Math.Min(targetListView.Items.Count - 1, (int)(position.Y / itemHeight));

            // Find the item being dropped on top of.
            ListViewItem targetItem = (ListViewItem)targetListView.ContainerFromIndex(index);

            // If the drop point is more than halfway down the item being dropped on...
            Point positionInItem = e.GetPosition(targetItem);
            if (positionInItem.Y > itemHeight / 2)
            {
                // Increase the drop index to be below the item being dropped on.
                index++;
            }

            // Avoid going out of bounds (default to placing the item at the end of the
            //      list, if the index is too large).
            index = Math.Min(targetListView.Items.Count, index);

            return index;
        }
    }
}
