using ButlerToolContract.DataTypes;
using ButlerSDK.ToolSupport;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using UnitTests.UnitTestingTools;

namespace UnitTests.CurrentTests
{
    [TestClass]
    public class ToolResolver5_UnitTests
    {
        /// <summary>
        /// Can we create an instance of our tool scheduler
        /// </summary>
        [TestMethod]
        public void ToolResolver5_CanMakeInstance_ShouldNotBeNull()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
        }

        /// <summary>
        /// Can we make tool scheduler instance, add a dummy tool, see if the flags <see cref="ToolResolver.HasScheduledTools"/> + <see cref="ToolResolver.ScheduledToolCount"/> are accurate
        /// </summary>
        [TestMethod]
        public void ScheduleTool_ToolCount_ShouldBeOne()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
        
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
        }

        [TestMethod]
        public void ScheduleTool_HasScheduledTools_ShouldBeTrue()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);
        }






        /// <summary>
        /// This routine adds a few tools, tries running a schedule with a null tool kit passed that the schedule routine rightfully rejects with <see cref="ArgumentNullException"/> <see cref="ToolResolver.RunSchedule(ButlerToolBench)"/>
        /// </summary>
        [TestMethod]
        public void ScheduleTool_ScheduleOneTool_ShouldNotThrow_ShouldCreateTwoMessages()
        {
            string TESTID = "TESTID";
            string FUNCTIONARGS = "NO ARGS";
            string TOOLNAME = "TOOLNAME";
            // create the mock and schedule

            var mockToolBox = new MockToolBox();
            
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");

            
            // validate sanity
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            // resolved tool should be zero. Once a tool call is done, this ticks up in a buffer
            Assert.IsTrue(TestMe.ResolvedToolCount == 0);
            // add a new tool call of 
            TestMe.ScheduleTool(TESTID, FUNCTIONARGS, TOOLNAME);


            // validate the scheduled tool flag and count is right
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);


            // run the fake call
            var result = TestMe.RunScheduleAsync(mockToolBox);
            result.Wait(); // and wait

            Assert.IsTrue(result.Status == TaskStatus.RanToCompletion); // no errors
            Assert.IsTrue(TestMe.HasScheduledTools == false); // no more tools to schedule

            Assert.IsTrue(TestMe.ResolvedToolCount == 1); // tool resolver called one tool request
            List<ButlerChatMessage> results = new(); 
            TestMe.PlaceInChatLog(results); // move the results to the look to peek


            Assert.IsTrue(results.Count == 2); // because resolver is trying to stay openAI standard in terms of call and result.
                                               // for each tool call, the tool will get a paired result message and same call id


            /* our case though is similar. Check type and order*/
            Assert.IsTrue(results[0].GetType() == typeof(ButlerChatToolCallMessage));
            Assert.IsTrue(results[1].GetType() == typeof(ButlerChatToolResultMessage));

            // get the actual types
            ButlerChatToolCallMessage? callMessage = results[0] as ButlerChatToolCallMessage;
            ButlerChatToolResultMessage? callResults = results[1] as ButlerChatToolResultMessage;

            Assert.IsNotNull(callMessage);
            Assert.IsNotNull(callResults);

            // did the scheduled tool string(s) propagate anyway
            Assert.IsTrue(string.Equals(callMessage.FunctionArguments, FUNCTIONARGS) );
            Assert.IsTrue(string.Equals(callMessage.ToolName, TOOLNAME));
            Assert.IsTrue(string.Equals(callMessage.Id, TESTID));


            // did id propagate ok?
            Assert.IsTrue(string.Equals(callResults.Id, TESTID));
            Assert.IsTrue(string.Equals(callResults.Id, callMessage.Id));

            Assert.IsTrue(callMessage.Role == ButlerChatMessageRole.ToolCall);
            Assert.IsTrue(callResults.Role == ButlerChatMessageRole.ToolResult);

            ;
            
        }

        /// <summary>
        /// This routine adds a few tools, tries running a schedule with a null tool kit passed that the schedule routine rightfully rejects with <see cref="ArgumentNullException"/> <see cref="ToolResolver.RunSchedule(ButlerToolBench)"/>
        /// </summary>
        [TestMethod]
        public void Basic_ScheduleThrows_IfToolKitNull_ScheduleCall_Only1Arg_ASynch()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);

            Assert.IsTrue(TestMe.ScheduledToolCount == 1);

            Task T = TestMe.RunScheduleAsync(null!); // null ok here cause we want it to go boom.
            Task.WaitAny(T);
            if (T.Status != TaskStatus.Faulted)
            {
                Assert.Fail("Task did not successfully see that the exception in RunScheduleAsync() actually happened");
            }
        }

        /// <summary>
        /// This routine adds a few tools, tries running a schedule with a null tool kit passed that the schedule routine rightfully rejects with <see cref="ArgumentNullException"/> <see cref="ToolResolver.RunSchedule(ButlerToolBench)"/>
        /// </summary>
        [TestMethod]
        public void Basic_ScheduleThrows_IfToolKitNull_ScheduleCall_Only1Arg_ASync_DONOW()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);

            Assert.IsTrue(TestMe.ScheduledToolCount == 1);

            Task T = TestMe.RunScheduleAsyncDoNow(null!); // null here cause we want boom
            Task.WaitAny(T);
            if (T.Status != TaskStatus.Faulted)
            {
                Assert.Fail("Task did not successfully see that the exception in RunScheduleAsyncDoNow() actually happened");
            }

        }
        /// <summary>
        /// create tool scheduler 5, add 1 tools, test a few flags, attempt to remove single tool via specific name (the called routine currently not implemented) and verify it does indeed throw that exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(NotSupportedException))]
        public void Basic_CanRemoveTool_NOTIMPLEMENTED()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);

            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            TestMe.RemoveTool("TESTID"); // NOTICE: This routine throws not implemented exception. I do plan on adding that feature: one d
            Assert.Fail("Either the RemoveTool function actually does that now in the scheduler or someone removed the exception throw. You should check toolResolver5.RemoveTool()");
        }

        /// <summary>
        /// create tool scheduler 5, add 2 tools, tests a few flags, remove tools via <see cref="ToolResolver.RemoveAllTool"/> which is done and test new flags
        /// </summary>
        [TestMethod]
        public void Basic_ADDTOOLS_RemoveAllTools()
        {
            var TestMe = ToolResolver.CreateSchedule("TESTSCHEDULE");
            Assert.IsNotNull(TestMe);
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            TestMe.ScheduleTool("TESTID", "NONE", "NULL");
            Assert.IsTrue(TestMe.ScheduledToolCount == 1);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);
            TestMe.ScheduleTool("TEST2", "NULLSECTOR", "NOARGS");
            Assert.IsTrue(TestMe.ScheduledToolCount == 2);
            Assert.IsTrue(TestMe.HasScheduledTools);
            Assert.IsFalse(TestMe.HasScheduledTools == false);
            Assert.IsTrue(TestMe.ResolvedToolCount == 0);



            TestMe.RemoveAllTool();
            Assert.IsTrue(TestMe.ScheduledToolCount == 0);
            Assert.IsTrue(!TestMe.HasScheduledTools);
            Assert.IsTrue(TestMe.HasScheduledTools == false);
            return;
        }
    }
}
