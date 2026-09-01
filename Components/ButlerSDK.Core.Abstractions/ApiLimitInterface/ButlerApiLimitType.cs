using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace ButlerSDK
{
    [Flags]

    public enum ButlerApiLimitType
    {
        /// <summary>
        /// no limit. This one trumps all others.
        /// </summary>
        none,
        /// <summary>
        /// Each call dec inv by 1. If 0 is left, further calls fails
        /// </summary>
        PerCall = 1,
        /// <summary>
        /// Each call dec budge by cost per call. If budget can't support it, call fails.
        /// </summary>
        SharedBudget = 2
    }
}

