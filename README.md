# OxyPlot.Eto

[![NuGet](http://img.shields.io/nuget/v/OxyPlot.Eto.svg?style=flat)](https://www.nuget.org/packages/OxyPlot.Eto/)

This is a port of the WindowsForms backend of [OxyPlot](https://github.com/oxyplot/oxyplot) to [Eto.Forms](https://github.com/picoe/Eto). It does not require any native controls and therefore should run on all platforms supported by Eto.  
This project is forked from https://github.com/mostanes/OxyPlot.EtoForms.

## Quickstart

Use NuGet to install [`OxyPlot.Eto`](https://www.nuget.org/packages/OxyPlot.Eto/), then add a `OxyPlot.Eto.PlotView` Control to your Form or Container by using the following example:  
```cs
	this.Title = "My Eto Form";

	var myModel = new PlotModel { Title = "Example 1" };
	myModel.Series.Add(new FunctionSeries(Math.Cos, 0, 10, 0.1, "cos(x)"));
	var plotView = new PlotView() { Model = myModel };

	this.Content = plotView;
```

![](./quickstart.png)  
