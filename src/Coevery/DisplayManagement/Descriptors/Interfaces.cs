﻿using System;
using Coevery.Caching;
using Coevery.Events;

namespace Coevery.DisplayManagement.Descriptors {

    public interface IShapeTableManager : ISingletonDependency {
        ShapeTable GetShapeTable(string themeName);
    }

    public interface IShapeTableProvider : IDependency {
        void Discover(ShapeTableBuilder builder);
    }

    public interface IShapeTableEventHandler : IEventHandler {
        void ShapeTableCreated(ShapeTable shapeTable);
    }

    public interface IShapeTableMonitor : IDependency {
        void Monitor(Action<IVolatileToken> monitor);
    }

}
