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
        CheckBox _skia = new CheckBox() { Text = "Use Skia" };
        CheckBox _reversed = new CheckBox() { Text = "Reversed" };
        CheckBox _transposed = new CheckBox() { Text = "Transposed" };
        CheckBox _autorun = new CheckBox() { Text = "Autorun" };
        UITimer _timer = new UITimer() { Interval = 1 };
        Panel _plot_holder = new Panel();
        TreeGridView _tree;

        public MainForm()
        {
            InitializeComponent();
            Menu = null;

            _tree = new TreeGridView() { DataStore = CreateTreeItem(), Width=300 };
            _tree.Columns.Add(new GridColumn { DataCell = new TextBoxCell(0) });
            _tree.SelectionChanged += Tree_SelectionChanged;
            _tree.SelectedItem = GetNextItem(_tree.DataStore as TreeGridItem);
            _reversed.CheckedChanged += (o, e) => InitPlot();
            _transposed.CheckedChanged += (o, e) => InitPlot();
            _skia.CheckedChanged += (o, e) => UseSkia = _skia.Checked ?? false;
            _autorun.CheckedChanged += (o, e) => { if (_autorun.Checked.Value) _timer.Start(); else _timer.Stop(); };

            _timer.Elapsed += _timer_Elapsed;

            UseSkia = false;

            var rightside = new DynamicLayout(
                new DynamicRow(new DynamicControl() { Control = _plot_holder, YScale = true }),
                new DynamicLayout(new DynamicRow(null, _transposed, _reversed, _skia, _autorun))
                );

            Content = new DynamicLayout(new DynamicRow(_tree, rightside));
        }

        private void _timer_Elapsed(object sender, EventArgs e)
        {
            if (SelectedExample is null)
            {
                _tree.SelectedItem = GetNextItem(_tree.SelectedItem as TreeGridItem);
            }
            else if (_transposed.Enabled && (_transposed.Checked ?? false) == (_skia.Checked ?? false))
            {
                _transposed.Checked = !_transposed.Checked;
            }
            else if (_reversed.Enabled && (_reversed.Checked ?? false) == (_skia.Checked ?? false))
            {
                _reversed.Checked = !_reversed.Checked;
            }
            else if ((_skia.Checked = !_skia.Checked) == false)
            { 
                _tree.SelectedItem = GetNextItem(_tree.SelectedItem as TreeGridItem);
            }
        }
        ExampleLibrary.ExampleInfo SelectedExample
        {
            get { return (_tree.SelectedItem as TreeGridItem)?.Tag as ExampleLibrary.ExampleInfo; }
        }

        private void Tree_SelectionChanged(object sender, EventArgs e)
        {
            try
            {
                _transposed.Enabled = SelectedExample?.IsTransposable ?? false;
                _reversed.Enabled = SelectedExample?.IsReversible ?? false;
                InitPlot();
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.ToString(), "Example init failed", MessageBoxType.Error);
            }
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

        TreeGridItem CreateTreeItem()
        {
            var root = new TreeGridItem();

            var examples = ExampleLibrary.Examples.GetList().OrderBy(e => e.Category);

            TreeGridItem node = null;

            foreach (var ex in examples)
            {
                if (node == null || node.GetValue(0).ToString() != ex.Category)
                {
                    node = new TreeGridItem (ex.Category);
                    root.Children.Add(node);
                }

                var exnode = new TreeGridItem (ex.Title) { Tag = ex };
                node.Children.Add(exnode);
            }

            return root;
        }
        TreeGridItem GetNextItem (TreeGridItem seed)
        {
            if (seed == null)
            {
                seed = _tree.DataStore as TreeGridItem;
            }

            if (seed == _tree.DataStore)
            {
                seed = seed.Children[0] as TreeGridItem;
            }
            else
            {
                var parent = seed.Parent as TreeGridItem;

                var next = parent.Children.IndexOf(seed) + 1;

                if (next > 0 && next < parent.Children.Count)
                {
                    seed = parent.Children[next] as TreeGridItem;
                }
                else
                {
                    parent.Expanded = false;
                    return GetNextItem(parent);
                }
            }

            while (seed.Tag == null)
            {
                seed.Expanded = true;
                seed = seed.Children[0] as TreeGridItem;
            }

            return seed;
        }
    }
}
