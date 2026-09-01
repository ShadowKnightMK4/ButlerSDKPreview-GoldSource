using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerToolContract.DataTypes
{
    public interface IButlerCollectionResult<T>: IEnumerable<T>
    {

    }

    public interface IButlerAsynchCollectionResult<T> : IAsyncEnumerable<T>
    {
    }

}
