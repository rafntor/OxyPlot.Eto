using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Eto;
using Eto.Forms;
using System.Linq;
using System;

namespace TestApp
{
	public partial class MainForm : Form
	{
		public MainForm()
		{
			InitializeComponent();
			Menu = null;

			this.Title = "My Eto Form";

			var myModel = new PlotModel { Title = "Example 1", DefaultFont = Eto.Drawing.FontFamilies.Sans.Name };
			myModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
			var plotView = new PlotView() { Model = myModel };

			this.Content = plotView;

			this.MouseUp += (s, e) => 
			{ 
				plotView.Model = BuildPlotModel();

				Content = plotView;
			};
		}

		private static PlotModel BuildPlotModel()
		{
			var rand = new Random();

			var model = new PlotModel { Title = "Cake Type Popularity" };

			var cakePopularity = Enumerable.Range(1, 5).Select(i => rand.NextDouble()).ToArray();
			var sum = cakePopularity.Sum();
			var barItems = cakePopularity.Select(cp => RandomBarItem(cp, sum)).ToArray();
			var barSeries = new BarSeries
			{
				ItemsSource = barItems,
				LabelPlacement = LabelPlacement.Base,
				LabelFormatString = "{0:.00}%"
			};

			model.Series.Add(barSeries);

			model.Axes.Add(new CategoryAxis
			{
				Position = AxisPosition.Left,
				Key = "CakeAxis",
				ItemsSource = new[]
				 {
						  "Apple cake",
						  "Baumkuchen",
						  "Bundt Cake",
						  "Chocolate cake",
						  "Carrot cake"
					 }
			});
			return model;
		}
		private static BarItem RandomBarItem(double cp, double sum)
			 => new BarItem { Value = cp / sum * 100, Color = RandomColor() };

		private static OxyColor RandomColor()
		{
			var r = new Random();
			return OxyColor.FromRgb((byte)r.Next(255), (byte)r.Next(255), (byte)r.Next(255));
		}
	}
}
