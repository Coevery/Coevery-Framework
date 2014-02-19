using System.Collections.Generic;
using Coevery.DisplayManagement.Shapes;

namespace Coevery.DisplayManagement {
    public interface IShapeDisplay : IDependency {
        string Display(Shape shape);
        string Display(object shape);
        IEnumerable<string> Display(IEnumerable<object> shapes);
    }
}
