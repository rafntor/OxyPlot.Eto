# OxyPlot.Eto

[![Build](https://github.com/rafntor/OxyPlot.Eto/actions/workflows/dotnet.yml/badge.svg)](https://github.com/rafntor/OxyPlot.Eto/actions/workflows/dotnet.yml)
[![Quality Gate Status](https://sonarcloud.io/api/project_badges/measure?project=rafntor_OxyPlot.Eto&metric=alert_status)](https://sonarcloud.io/summary/new_code?id=rafntor_OxyPlot.Eto)
[![NuGet](http://img.shields.io/nuget/v/OxyPlot.Eto.svg)](https://www.nuget.org/packages/OxyPlot.Eto/)
[![License](https://img.shields.io/github/license/rafntor/OxyPlot.Eto)](LICENSE)

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
