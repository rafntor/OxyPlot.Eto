using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using OxyPlot.Eto;
using OxyPlot.Eto.Skia;
using Eto.Forms;
using System.Linq;
using System;

namespace TestApp
{
    public partial class MainForm : Form
    {
        CheckBox _reversed = new CheckBox() { Text = "Reversed" };
        CheckBox _transposed = new CheckBox() { Text = "Transposed" };
        Panel _plot_holder = new Panel();
        TreeView _tree;

        public MainForm()
        {
            InitializeComponent();

            _tree = new TreeView() { DataStore = CreateTreeItem(),Width=300 };
            _tree.SelectionChanged += Tree_SelectionChanged;
            _reversed.CheckedChanged += (o, e) => InitPlot();
            _transposed.CheckedChanged += (o, e) => InitPlot();

            UseSkia = false;

            var rightside = new DynamicLayout(
                new DynamicRow(new DynamicControl() { Control = _plot_holder, YScale = true }),
                new DynamicLayout(new DynamicRow(null, _transposed, _reversed))
                );

            Content = new DynamicLayout(new DynamicRow(_tree, rightside));
        }

        ExampleLibrary.ExampleInfo SelectedExample
        {
            get { return (_tree.SelectedItem as TreeItem)?.Tag as ExampleLibrary.ExampleInfo; }
        }

        private void Tree_SelectionChanged(object sender, EventArgs e)
        {
            _transposed.Enabled = SelectedExample?.IsTransposable ?? false;
            _reversed.Enabled = SelectedExample?.IsReversible ?? false;
            InitPlot();
        }

        void InitPlot()
        {
            var flags = ExampleLibrary.ExampleInfo.PrepareFlags(
                _transposed.Enabled && (_transposed.Checked ?? false),
                _reversed.Enabled && (_reversed.Checked ?? false));

            var ex = SelectedExample;

            var ctrl = ex?.GetController(flags);
            var model = ex?.GetModel(flags);

            if (_plot_holder.Content is OxyPlot.Eto.PlotView etoview)
            {
                etoview.Controller = ctrl;
                etoview.Model = model;
            }
            if (_plot_holder.Content is OxyPlot.Eto.Skia.PlotView skiaview)
            {
                skiaview.Controller = ctrl;
                skiaview.Model = model;
            }
        }

        bool UseSkia
        {
            get => _plot_holder.Content is OxyPlot.Eto.Skia.PlotView;
            set
            {
                (_plot_holder.Content as OxyPlot.Eto.PlotView)?.Dispose();
                (_plot_holder.Content as OxyPlot.Eto.Skia.PlotView)?.Dispose();

                _plot_holder.Content = value ? new OxyPlot.Eto.Skia.PlotView() : new OxyPlot.Eto.PlotView();

                Title = "OxyPlot.Eto.Demo / ExampleBrowser" + (UseSkia ? " (Skia)" : "");

                InitPlot();
            }
        }

        TreeItem CreateTreeItem()
        {
            var root = new TreeItem();

            var examples = ExampleLibrary.Examples.GetList().OrderBy(e => e.Category);

            TreeItem node = null;

            foreach (var ex in examples)
            {
                if (node == null || node.Text != ex.Category)
                {
                    node = new TreeItem { Text = ex.Category };
                    root.Children.Add(node);
                }

                var exnode = new TreeItem { Text = ex.Title, Tag = ex };
                node.Children.Add(exnode);
            }

            return root;
        }
    }
}
