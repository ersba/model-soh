﻿using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Threading;
using Mars.Common.Core.Logging;
using Mars.Components.Starter;
using Mars.Core.Simulation;
using Mars.Interfaces;
using Mars.Interfaces.Environments;
using Mars.Interfaces.Model;
using SOHModel.Bicycle.Rental;
using SOHModel.Car.Model;
using SOHModel.Car.Parking;
using SOHModel.Domain.Graph;
using SOHModel.Multimodal.Layers;
using SOHModel.Multimodal.Layers.TrafficLight;
using SOHModel.Multimodal.Model;

namespace SOHInfectedBergedorf;

internal static class Program
{
    private static void Main(string[] args)
    {
        var watch = Stopwatch.StartNew();
        Thread.CurrentThread.CurrentCulture = new CultureInfo("EN-US");
        LoggerFactory.SetLogLevel(LogLevel.Off);

        var description = new ModelDescription();

        // description.AddLayer<StreetLayer>(new[] { typeof(ISpatialGraphLayer) });
        // description.AddLayer<SidewalkLayer>();
        description.AddLayer<SpatialGraphMediatorLayer>(new[] { typeof(ISpatialGraphLayer) });

        description.AddLayer<CarParkingLayer>(new[] { typeof(ICarParkingLayer) });
        description.AddLayer<BicycleRentalLayer>(new[] { typeof(IBicycleRentalLayer) });
        description.AddLayer<VectorBuildingsLayer>();
        description.AddLayer<VectorLanduseLayer>();
        description.AddLayer<VectorPoiLayer>();
        description.AddLayer<MediatorLayer>();
        description.AddLayer<CitizenLayer>();

        description.AddAgent<Citizen, CitizenLayer>();

        description.AddEntity<Car>();
        description.AddEntity<RentalBicycle>();

        ISimulationContainer application;
        if (args != null && args.Any())
        {
            application = SimulationStarter.BuildApplication(description, args);
        }
        else
        {
            var config = GetConfig();
            Console.WriteLine("config Datei: " + config);
            application = SimulationStarter.BuildApplication(description, config);
        }

        var simulation = application.Resolve<ISimulation>();


        var state = simulation.StartSimulation();
        watch.Stop();

        Console.WriteLine($"Input/Initialization phase lasted:   {state.InputWatch.ElapsedMilliseconds}");
        Console.WriteLine($"Computing phase lasted:              {state.TickWatch.ElapsedMilliseconds}");
        Console.WriteLine($"Output/Write phase lasted:           {state.OutputWatch.ElapsedMilliseconds}");
        Console.WriteLine($"Complete execution lasted:           {watch.ElapsedMilliseconds}");
    }

    private static SimulationConfig GetConfig()
    {
        SimulationConfig simulationConfig;
        var configValue = Environment.GetEnvironmentVariable("CONFIG");
        
        if (configValue != null)
        {
            Console.WriteLine("Use passed simulation config by environment variable");
            simulationConfig = SimulationConfig.Deserialize(configValue);
            Console.WriteLine(simulationConfig.Serialize());
        }
        else
        {
            var file = File.ReadAllText("config.json");
            simulationConfig = SimulationConfig.Deserialize(file);
        }

        return simulationConfig;

        var startPoint = DateTime.Parse("2020-01-01T00:00:00");
        var config = new SimulationConfig
        {
            Globals =
            {
                StartPoint = startPoint,
                EndPoint = startPoint + TimeSpan.FromHours(12),
                DeltaTUnit = TimeSpanUnit.Seconds,
                ShowConsoleProgress = true,
                LiveVisualization = false,
                SqLiteOptions =
                {
                    DistinctTable = false
                }
            },
            LayerMappings =
            {
                new LayerMapping
                {
                    Name = nameof(TrafficLightLayer),
                    File = Path.Combine("resources", "traffic_lights_altona.zip")
                },
                new LayerMapping
                {
                    Name = nameof(VectorBuildingsLayer),
                    File = Path.Combine("resources", "Buildings_Altona_altstadt.geojson")
                },
                new LayerMapping
                {
                    Name = nameof(VectorLanduseLayer),
                    File = Path.Combine("resources", "Landuse_Altona_altstadt.geojson")
                },
                new LayerMapping
                {
                    Name = nameof(VectorPoiLayer),
                    File = Path.Combine("resources", "POIS_Altona_altstadt.geojson")
                },
                new LayerMapping
                {
                    Name = nameof(SpatialGraphMediatorLayer),
                    Inputs = new List<Input>
                    {
                        new Input
                        {
                            File = Path.Combine("resources", "drive_graph_altona_altstadt.graphml"),
                            InputConfiguration = new InputConfiguration
                            {
                                Modalities = new HashSet<SpatialModalityType>
                                {
                                    SpatialModalityType.CarDriving, SpatialModalityType.Cycling
                                }
                            }
                        },
                        new Input
                        {
                            File = Path.Combine("resources", "walk_graph_altona_altstadt.graphml"),
                            InputConfiguration = new InputConfiguration
                            {
                                Modalities = new HashSet<SpatialModalityType>{ SpatialModalityType.Walking }
                            }
                        },
                        
                    }
                },
                new LayerMapping
                {
                    Name = nameof(CarParkingLayer),
                    File = Path.Combine("resources", "Parking_Altona_altstadt.geojson")
                },
                new LayerMapping
                {
                    Name = nameof(BicycleRentalLayer),
                    OutputTarget = OutputTargetType.None,
                    File = Path.Combine("resources", "Bicycle_Rental_Altona_altstadt.geojson")
                },
                new LayerMapping
                {
                    Name = nameof(CitizenLayer),
                    IndividualMapping =
                    {
                        new IndividualMapping { Name = "ParkingOccupancy", Value = 0.779 },
                    }
                }
            },
            AgentMappings =
            {
                new AgentMapping
                {
                    Name = nameof(Citizen),
                    InstanceCount = 10000,
                    OutputTarget = OutputTargetType.SqLite,
                    File = Path.Combine("resources", "citizen_init_10k.csv"),
                    Outputs = new List<Output>
                    {
                        new Output
                        {
                            OutputTarget = OutputTargetType.Trips
                        }
                    },
                    OutputFilter =
                    {
                        new OutputFilter
                        {
                            Name = "StoreTickResult",
                            Values = new object[] { true },
                            Operator = ContainsOperator.In
                        }
                    },
                    IndividualMapping =
                    {
                        new IndividualMapping { Name = "ResultTrajectoryEnabled", Value = true },
                        new IndividualMapping { Name = "CapabilityDriving", Value = true },
                        new IndividualMapping { Name = "CapabilityCycling", Value = true }
                    }
                }
            },
            EntityMappings =
            {
                new EntityMapping
                {
                    Name = nameof(RentalBicycle),
                    File = Path.Combine("resources", "bicycle.csv")
                },
                new EntityMapping
                {
                    Name = nameof(Car),
                    File = Path.Combine("resources", "car.csv")
                }
            }
        };

        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine(config.Serialize());
        Console.WriteLine();
        Console.WriteLine();
        Console.WriteLine();
        return config;
    }
}