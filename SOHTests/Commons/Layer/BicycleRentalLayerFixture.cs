using System;
using Mars.Interfaces.Data;
using Mars.Interfaces.Environments;
using SOHModel.Bicycle.Rental;
using SOHModel.Domain.Graph;

namespace SOHTests.Commons.Layer;

public class BicycleRentalLayerFixture : IDisposable
{
    public BicycleRentalLayerFixture(ISpatialGraphEnvironment environment)
    {
        BicycleRentalLayer = new BicycleRentalLayer
        {
            SpatialGraphMediatorLayer = new SpatialGraphMediatorLayer { Environment = environment }
        };
        BicycleRentalLayer.InitLayer(
            new LayerInitData
            {
                LayerInitConfig =
                {
                    File = ResourcesConstants.BicycleRentalAltonaAltstadt
                }
            });
    }

    public BicycleRentalLayer BicycleRentalLayer { get; }

    public void Dispose()
    {
        BicycleRentalLayer.Dispose();
    }
}