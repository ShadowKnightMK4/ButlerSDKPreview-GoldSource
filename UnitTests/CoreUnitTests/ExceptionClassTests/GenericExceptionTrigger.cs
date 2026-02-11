using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace UnitTests.ExceptionClassTests
{
    [TestClass]
    public class ToolAlreadyExistsException_Tests
    {
        [TestMethod]
        public void MakeInstance_NoArgs_ShouldBeNotNull()
        {
            var toolexists = new ToolAlreadyExistsException();
            Assert.IsNotNull(toolexists);
            Assert.IsInstanceOfType(toolexists, typeof(ToolAlreadyExistsException));
        }

        [TestMethod]
        public void MakeInstance_OneArg_ShouldBeNotNull()
        {
            var toolexists = new ToolAlreadyExistsException("test");
            Assert.IsNotNull(toolexists);
            Assert.IsInstanceOfType(toolexists, typeof(ToolAlreadyExistsException));
            Assert.IsTrue(toolexists.Message.Contains("test"));
        }

    }

    [TestClass]
    public class ToolNotFoundException_Tests
    {
        [TestMethod]
        public void MakeInstance_NoArgs_ShouldBeNotNull()
        {
            var toolexists = new ToolNotFoundException();
            Assert.IsNotNull(toolexists);
            Assert.IsInstanceOfType(toolexists, typeof(ToolNotFoundException));
        }

        [TestMethod]
        public void MakeInstance_OneArg_ShouldBeNotNull()
        {
            var toolexists = new ToolNotFoundException("test");
            Assert.IsNotNull(toolexists);
            Assert.IsInstanceOfType(toolexists, typeof(ToolNotFoundException));
            Assert.IsTrue(toolexists.Message.Contains("test"));
        }

    }
}
