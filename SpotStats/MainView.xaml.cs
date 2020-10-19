using OxyPlot;
using OxyPlot.Axes;
using OxyPlot.Series;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Xml;
using VMS.TPS.Common.Model.API;

namespace SpotStats
{
    /// <summary>
    /// Interaction logic for MainView.xaml
    /// </summary>
    public partial class MainView : UserControl
    {
        private ScriptContext context;
        private IonPlanSetup plan;
        private IonBeam beam;
        private List<LayerView> layerViews;
        private double beamXMax;
        private double beamXMin;
        private double beamYMax;
        private double beamYMin;
        public MainView()
        {
            InitializeComponent();
        }

        public MainView(ScriptContext context)
        {
            InitializeComponent();
            this.context = context;
            this.plan = context.IonPlanSetup;
            this.txtPlan.Text = $"Plan: {this.plan.Id}";
            cbField.ItemsSource = this.plan.IonBeams;
        }

        private void cbField_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            layerViews = new List<LayerView>();
            this.beam = (IonBeam)cbField.SelectedItem;
            var starticp = beam.IonControlPoints.Where((p, i) => i % 2 == 0);
            // var endicp = beam.IonControlPoints.Where((p, i) => i % 2 == 1);
            //foreach (var icpPair in starticp.Zip(endicp, (st, en) => new { start = st, end = en }))
            foreach (var icp in starticp)
                layerViews.Add(new LayerView(icp, beam.Meterset.Value, plan.PlannedDosePerFraction.Dose / 100));
            beamXMax = layerViews.Max(l => l.xmax);
            beamXMin = layerViews.Min(l => l.xmin);
            beamYMax = layerViews.Max(l => l.ymax);
            beamYMin = layerViews.Min(l => l.ymin);
            lvLayer.ItemsSource = layerViews;
            pvEnergy.Model = createEnergyPlotModel(layerViews);
        }

        private PlotModel createEnergyPlotModel(List<LayerView> layerViews)
        {
            PlotModel pm = new PlotModel() { Title = $"Spot Stats for plan {this.plan.Id}" };
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "(%)" });
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "Energy" });
            var s = new StemSeries { Title = beam.Id, MarkerStroke = OxyColors.Blue, MarkerType = MarkerType.Circle, TrackerFormatString= "{0}\n{1}: {2:0.00}MeV\n{3}: {4:0.00}%" };
            foreach(var lv in layerViews)
            {
                DataPoint dp = new DataPoint(lv.Energy, lv.MUpercent);
                s.Points.Add(dp);
            }
            pm.Series.Add(s);
            return pm;
        }

        private void lvLayer_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if(lvLayer.SelectedItem != null)
                pvSpot.Model = createSpotPlotModel((LayerView)lvLayer.SelectedItem);
        }

        private PlotModel createSpotPlotModel(LayerView layerView)
        {
            PlotModel pm = new PlotModel() { Title = $"{layerView.Energy:0.00}MeV" };
            pm.Axes.Add(new LinearColorAxis() { Title="MU", Minimum = layerView.MinMU, Maximum = layerView.MaxMU, Position = AxisPosition.None, Key = "ColorAxis" });
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Left, Title = "Y(mm)", Minimum = beamYMin, Maximum = beamYMax });
            pm.Axes.Add(new LinearAxis { Position = AxisPosition.Bottom, Title = "X(mm)", Minimum = beamXMin, Maximum = beamXMax });
            var s = new ScatterSeries() { Title = beam.Id, MarkerType = MarkerType.Circle, ColorAxisKey = "ColorAxis", MarkerSize = 4, TrackerFormatString= "{0}\n{1}: {2:0.0}\n{3}: {4:0.0}\n{5}: {6:0.00}" };
            foreach(var spot in layerView.Spots)
            {
                ScatterPoint sp = new ScatterPoint(spot.Position.x, spot.Position.y, double.NaN, spot.Weight * layerView.MU_weight_ratio);
                s.Points.Add(sp);
            }
            pm.Series.Add(s);
            return pm;
        }
    }

    public class LayerView
    {
        public double MU_weight_ratio { get; set; }
        public double Energy { get; set; }
        public int SpotNo { get; set; }
        public double TotalMU { get; set; }
        public double MaxMU { get; set; }
        public double MinMU { get; set; }
        public double MUpercent { get; set; }
        public IonSpotCollection Spots { get; set; }
        public double xmax { get; set; }
        public double xmin { get; set; }
        public double ymax { get; set; }
        public double ymin { get; set; }
        public LayerView(IonControlPoint starticp, double beamMU, double MU_weight_ratio)
        {
            this.MU_weight_ratio = MU_weight_ratio;
            this.Energy = starticp.NominalBeamEnergy;
            this.SpotNo = starticp.FinalSpotList.Count();
            this.TotalMU = starticp.FinalSpotList.Sum(s => s.Weight) * MU_weight_ratio;
            this.MaxMU = starticp.FinalSpotList.Max(s => s.Weight) * MU_weight_ratio;
            this.MinMU = starticp.FinalSpotList.Min(s => s.Weight) * MU_weight_ratio;
            this.MUpercent = this.TotalMU / beamMU * 100;
            this.Spots = starticp.FinalSpotList;
            this.xmax = Spots.Max(s => s.Position.x);
            this.xmin = Spots.Min(s => s.Position.x);
            this.ymax = Spots.Max(s => s.Position.y);
            this.ymin = Spots.Min(s => s.Position.y);
        }
    }
}
