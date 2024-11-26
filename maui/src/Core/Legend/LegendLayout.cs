﻿using System.Collections.Specialized;

namespace Syncfusion.Maui.Toolkit.Internals
{
	/// <summary>
	/// Represents a layout for displaying legends, inheriting from <see cref="AbsoluteLayout"/>.
	/// </summary>
	internal class LegendLayout : AbsoluteLayout
    {
        #region Fields

        ILegend? _legend;
        IPlotArea _plotArea;
        SfLegend? _legendItemsView;
		Rect previousBounds;
		internal readonly AreaBase AreaBase;

		#endregion

		#region Constructor

		/// <summary>
		/// Initializes a new instance of the <see cref="LegendLayout"/> class with the specified area.
		/// </summary>
		/// <param name="area">The area associated with the legend layout.</param>
		public LegendLayout(AreaBase? area)
        {
            if (area == null)
                throw new ArgumentNullException("Chart area cannot be null");

            AreaBase = area;
            if (AreaBase is not IView chartAreaView)
                throw new ArgumentException("Chart area should be a view");

            _plotArea = AreaBase.PlotArea;

            //Todo: Need to check collection changes needed.
            var legendItems = _plotArea.LegendItems as INotifyCollectionChanged;
            if (legendItems != null)
                legendItems.CollectionChanged += OnLegendItemsCollectionChanged;

            _plotArea.LegendItemsUpdated += OnLegendItemsUpdated;

            Add(chartAreaView);
        }

		#endregion

		#region Properties

		/// <summary>
		/// Gets or sets the legend.
		/// </summary>
		public ILegend? Legend
        {
            get
            {
                return _legend;
            }
            internal set
            {
                if (_legend != value)
                {
                    _legend = value;
                    _plotArea.Legend = value;
                    CreateLegendView();
                }
            }
        }

		#endregion

		#region Methods

		/// <summary>
		/// Arranges the content of the layout within the specified bounds.
		/// </summary>
		/// <param name="bounds">The rectangle that defines the bounds of the layout.</param>
		/// <returns>The final size of the arranged content.</returns>
		protected override Size ArrangeOverride(Rect bounds)
        {
			//Calculate LegendItems before calling AreaBase's layout.
			if (previousBounds != bounds)
			{
				_plotArea.UpdateLegendItems();
				UpdateLegendLayout(bounds);
				previousBounds = bounds;
			}

			return base.ArrangeOverride(bounds);
        }

        void UpdateLegendLayout(Rect bounds)
        {
            var areaBounds = new Rect(0, 0, bounds.Width, bounds.Height);

            if (_legend != null)
            {
                if (_legendItemsView != null)
                {
                    var legendRectangle = SfLegend.GetLegendRectangle(_legendItemsView, new Rect(0, 0, bounds.Width, bounds.Height),
GetMaximumCoeff(_legend.ItemsMaximumHeightRequest?.Invoke() ?? 0.25));

                    if (_legendItemsView.Placement == LegendPlacement.Top)
                    {
                        AbsoluteLayout.SetLayoutBounds(_legendItemsView, new Rect(0, 0, bounds.Width, legendRectangle.Height));

                        areaBounds = new Rect(0, legendRectangle.Height, bounds.Width, bounds.Height - legendRectangle.Height);

                    }
                    else if (_legendItemsView.Placement == LegendPlacement.Bottom)
                    {
                        AbsoluteLayout.SetLayoutBounds(_legendItemsView, new Rect(0, bounds.Height - legendRectangle.Height, bounds.Width, legendRectangle.Height));

                        areaBounds = new Rect(0, 0, bounds.Width, bounds.Height - legendRectangle.Height);
                    }
                    else if (_legendItemsView.Placement == LegendPlacement.Left)
                    {
                        AbsoluteLayout.SetLayoutBounds(_legendItemsView, new Rect(0, 0, legendRectangle.Width, bounds.Height));
                        areaBounds = new Rect(legendRectangle.Width, 0, bounds.Width - legendRectangle.Width, bounds.Height);
                    }
                    else if (_legendItemsView.Placement == LegendPlacement.Right)
                    {
                        AbsoluteLayout.SetLayoutBounds(_legendItemsView, new Rect(bounds.Width - legendRectangle.Width, 0, legendRectangle.Width, bounds.Height));
                        areaBounds = new Rect(0, 0, bounds.Width - legendRectangle.Width, bounds.Height);
                    }

                    AbsoluteLayout.SetLayoutBounds(AreaBase, areaBounds);
                }
            }
            else
            {
                AbsoluteLayout.SetLayoutBounds(AreaBase, areaBounds);
            }
        }

        void OnLegendItemsCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {

        }

        double GetMaximumCoeff(double value)
        {
            if (!double.IsNaN(value))
            {
                return value > 1 ? 1 : value < 0 ? 0 : value;
            }

            return 0.25d;
        }

        void OnLegendItemsUpdated(object? sender, EventArgs e)
        {
            if (Legend != null && !Bounds.IsEmpty)
            {
                //Todo: For Placement and Oriention property changes
                //
                //legendItemsView.Orientation = Legend.Orientation;
                //legendItemsView.Placement = Legend.Placement;
                //legendItemsView.ToggleVisibility = Legend.ToggleVisibility;
                if(_legendItemsView != null)
                    _legendItemsView.ItemsSource = _plotArea.LegendItems;

                UpdateLegendLayout(Bounds);
            }
        }

        void CreateLegendView()
        {
            //Todo: We need to reimplement the legend feature in chart, once fixed the Bindable layout issue and dynamically change scroll view content issue.
            //https://github.com/dotnet/maui/issues/1393
            if (_legendItemsView != null)
                Remove(_legendItemsView);

            if (Legend != null)
            {
                var itemsView = Legend.CreateLegendView();
                _legendItemsView = itemsView ?? new SfLegend();
                _legendItemsView.BindingContext = _legend;
                _legendItemsView.SetBinding(SfLegend.ToggleVisibilityProperty, static (SfLegend sfLegend) => sfLegend.ToggleVisibility);
                _legendItemsView.SetBinding(SfLegend.PlacementProperty, static (SfLegend sfLegend) => sfLegend.Placement);
                _legendItemsView.SetBinding(SfLegend.ItemTemplateProperty, static (SfLegend sfLegend) => sfLegend.ItemTemplate);
                _legendItemsView.SetBinding(SfLegend.IsVisibleProperty, static (SfLegend sfLegend) => sfLegend.IsVisible);_legendItemsView.SetBinding(SfLegend.ItemsLayoutProperty, static (SfLegend sfLegend) => sfLegend.ItemsLayout);

                _legendItemsView.ItemsSource = _plotArea.LegendItems;
                _legendItemsView.ItemClicked += OnLegendItemToggled;
                _legendItemsView.PropertyChanged += LegendItemsView_PropertyChanged;
                ((ILegend)_legendItemsView).ItemsMaximumHeightRequest = Legend.ItemsMaximumHeightRequest;
                Add(_legendItemsView);

				if (!Bounds.IsEmpty && (Bounds.Width > -1 || Bounds.Height > -1))
					UpdateLegendLayout(Bounds);
			}
			else
			{
				if (!Bounds.IsEmpty && (Bounds.Width > -1 || Bounds.Height > -1))
					UpdateLegendLayout(Bounds);
			}
		}

        void LegendItemsView_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
        {

#if MACCATALYST || IOS
            _legendItemsView?.UpdateRelayout();
#endif

            if (e.PropertyName == nameof(SfLegend.Placement))
            {
                UpdateLegendLayout(Bounds);
            }
        }

        void OnLegendItemToggled(object? sender, LegendItemClickedEventArgs e)
        {
            ToggleLegendItem(e.LegendItem);
        }

        void ToggleLegendItem(ILegendItem? legendItem)
        {
            if (legendItem != null)
            {
                _plotArea.LegendItemToggleHandler.Invoke(legendItem);
            }
        }

        #endregion
    }
}
