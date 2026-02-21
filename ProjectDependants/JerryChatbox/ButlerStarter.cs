using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK
{
    /// <summary>
    /// Factor Facade class to get started easier with Butler SDK. Full Power comes from using the other classes directly <see cref="Butler"/>
    /// </summary>
    public class ButlerStarter
    {
        public static readonly ButlerStarter Instance = new ButlerStarter();
    }
}
