﻿// (c) Copyright Microsoft Corporation.
// This source is subject to the Microsoft Public License (Ms-PL).
// Please see http://go.microsoft.com/fwlink/?LinkID=131993 for details.
// All other rights reserved.

using System.Collections.ObjectModel;

#if MIGRATION
namespace System.Windows.Controls.DataVisualization.Charting
#else
namespace Windows.UI.Xaml.Controls.DataVisualization.Charting
#endif
{
    /// <summary>
    /// An axis interface used to determine the plot area coordinate of values.
    /// </summary>
    [OpenSilver.NotImplemented]
    public interface IAxis
    {
        /// <summary>
        /// Gets or sets the orientation of the axis.
        /// </summary>
        AxisOrientation Orientation { get; set; }

        /// <summary>
        /// This event is raised when the Orientation property is changed.
        /// </summary>
        event RoutedPropertyChangedEventHandler<AxisOrientation> OrientationChanged;

        /// <summary>
        /// Returns a value indicating whether the axis can plot a value.
        /// </summary>
        /// <param name="value">The value to plot.</param>
        /// <returns>A value indicating whether the axis can plot a value.
        /// </returns>
        bool CanPlot(object value);

        /// <summary>
        /// Gets the collection of child axes.
        /// </summary>
        ObservableCollection<IAxis> DependentAxes { get; }
    }
}