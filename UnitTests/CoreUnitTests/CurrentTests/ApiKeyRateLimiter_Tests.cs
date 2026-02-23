using ButlerSDK;


namespace UnitTests.CurrentTests
{
    [TestClass]
    public class ApiKeyRateLimiter_Tests
    {
        /// <summary>
        /// Ensure we can create an instance of this class without the kaboom
        /// </summary>
        [TestMethod]
        public void CanInstance_ShouldBeNotNull()
        {
            Assert.IsNotNull(new ApiKeyRateLimiter());
        }

        /// <summary>
        /// Can we add a service, it doesn't go boom and test if it exists and count = 1
        /// </summary>
        [TestMethod]
        public void Basic_CanAddService_TestCount_ShouldBeOne()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Test.AddService("SERVICENAME", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Assert.IsTrue(Test.ServiceCount == 1);
        }

        /// <summary>
        /// Can we add a service, it doesn't go boom and test if it exists and count = 1
        /// </summary>
        [TestMethod]
        public void Basic_CanAddService_TestExists_ShouldBeTrue()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Test.AddService("SERVICENAME", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Assert.IsTrue(Test.DoesServiceExist("SERVICENAME"));
        }


        /// <summary>
        /// Attempt to add 2 services with the same name without opting into replacing service. This should trigger an invalid operation exception
        /// </summary>
        [TestMethod]
        [ExpectedException(typeof(InvalidOperationException))]
        public void Basic_AddServiceTwice_ReplaceFlagFalse_ThrowsException()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Test.AddService("SERVICENAME", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Assert.IsTrue(Test.DoesServiceExist("SERVICENAME"));
            Assert.IsTrue(Test.ServiceCount == 1);
            Test.AddService("SERVICENAME", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
        }

        /// <summary>
        /// Add 2 services to the collection, with the replace if true flag set to true. Second service orrides 1st one. A test to service cost is don
        /// </summary>
        [TestMethod]
        public void Basic_AddServiceTwice_ReplaceFlagTrue_ShouldBeNoExeception()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Test.AddService("SERVICENAME", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Assert.IsTrue(Test.DoesServiceExist("SERVICENAME"));
            Assert.IsTrue(Test.ServiceCount == 1);
            Test.AddService("SERVICENAME", 25.12m , ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.SharedBudget, true);

            Assert.IsTrue(Test.DoesServiceExist("SERVICENAME"));
            Assert.IsTrue(Test.ServiceCount == 1);
            Assert.IsTrue(Test.GetCurrentCost("SERVICENAME") == 25.12m);

        }

        /// <summary>
        /// Add 2 servives to a list, test if they exist, test if count is 2
        /// </summary>
        [TestMethod]
        public void Basic_TestForServiceExist()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Assert.IsTrue(Test.ServiceCount == 1);
            Test.AddService("REAPER", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);

            Assert.IsTrue(Test.DoesServiceExist("REAPER"));
            Assert.IsTrue(Test.DoesServiceExist("SOMBRA"));
            Assert.IsTrue(Test.ServiceCount == 2);
        }

        /// <summary>
        /// Add 2 services to list, test if they exist, verify accurate count, remove one at a time, verify accurate account each time
        /// </summary>

        [TestMethod]
        public void Basic_AddService_TestExist_Remove_TestIfNotThere()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);
            Test.AddService("REAPER", 0, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.none, false);

            Assert.IsTrue(Test.DoesServiceExist("REAPER"));
            Assert.IsTrue(Test.DoesServiceExist("SOMBRA"));
            Assert.IsTrue(Test.ServiceCount == 2);

            Test.RemoveService("SOMBRA");
            Assert.IsFalse(Test.DoesServiceExist("SOMBRA"));
            Assert.IsFalse(Test.ServiceCount == 2);
            Assert.IsTrue(Test.ServiceCount ==1);

            Assert.IsTrue(Test.DoesServiceExist("REAPER"));
            Test.RemoveService("REAPER");
            Assert.IsFalse(Test.DoesServiceExist("REAPER"));
            Assert.IsFalse(Test.ServiceCount == 1);
            Assert.IsTrue(Test.ServiceCount == 0);
        }

        /// <summary>
        /// Create a service that costs 20 shared budget. Charge one call.  Confirm the shared budget was adjusted post call accurately.
        /// </summary>
        [TestMethod]
        public void Basic_ChargeService_TestLimit_SharedBudget_UnderBudget()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 20, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.SharedBudget, false);
            Assert.IsTrue(Test.ServiceCount == 1);
            Test.SharedBudget = 200;
            Assert.IsTrue(Test.SharedBudget == 200);

            Assert.IsTrue(Test.CheckForCallPermission("SOMBRA", 1));
            Test.ChargeService("SOMBRA", 1);
            Assert.IsTrue(Test.SharedBudget == 180);

        }

        /// <summary>
        /// Create a service that costs 500 shared budget. set shared budget to be smalled than 500. Share 1 call.  Code should throw over budget exception
        /// </summary>

        [TestMethod]
        [ExpectedException(typeof(ApiKeyRateLimiter.OverBudgetException))]
        public void Basic_ChargeService_TestLimit_SharedBudget_OverBudget()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Test.AddService("SOMBRA", 500, ulong.MaxValue, ulong.MaxValue, ButlerApiLimitType.SharedBudget, false);
            Test.SharedBudget = 200;
            Assert.IsTrue(Test.ServiceCount == 1);
            Assert.IsTrue(Test.SharedBudget == 200);
            Assert.IsTrue(Test.GetCurrentCost("SOMBRA") == 500);

            Assert.IsFalse(Test.CheckForCallPermission("SOMBRA", 20));
            Test.ChargeService("SOMBRA", 1);

            Assert.Fail("The expected exception didn't trigger. Budget is overlimit");
        }

        /// <summary>
        /// Create a service, set cost to be less than shared budget. call it. Does the call go thruy
        /// </summary>

        [TestMethod]
        public void Basic_ChargeService_TestLimt_CostPerCall_UnderBudget()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 200, 20, 40, ButlerApiLimitType.PerCall, false);
            Test.SharedBudget = 200;
            Assert.IsTrue(Test.ServiceCount == 1);
            Assert.IsTrue(Test.SharedBudget == 200);

            Assert.IsTrue(Test.CheckForCallPermission("SOMBRA", 1));
            Test.ChargeService("SOMBRA", 1);
            Assert.IsTrue(Test.GetServiceInventory("SOMBRA") == 19);
       }



        /// <summary>
        /// Create a service with inventory tracking. Try calling for more requess than the inventory has. This shoudl trigger Overbudget Exception
        /// </summary>
        [ExpectedException(typeof(ApiKeyRateLimiter.OverBudgetException))]
        [TestMethod]
        public void Basic_ChargeService_TestLimt_CostPerCallOverBudget()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 200, 20, 40, ButlerApiLimitType.PerCall, false);
            Test.SharedBudget = 200;
            Assert.IsTrue(Test.SharedBudget == 200);

            Assert.IsFalse(Test.CheckForCallPermission("SOMBRA", 21));

            Test.ChargeService("SOMBRA", 1);
            Test.ChargeService("SOMBRA", 20);
            Assert.Fail("The expected exception didn't trigger. inventory  is overlimit");
        }

        /// <summary>
        /// Create a new per call service, try assigning a new call limit
        /// </summary>
        [TestMethod]
        public void Basic_ChargeService_TestLimt_AssignNewLimit()
        {
            uint old_limit = 40;
            uint new_limit = 500;
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 200, 20, old_limit, ButlerApiLimitType.PerCall, false);

            Assert.IsTrue(Test.ServiceCount == 1);
            Assert.IsTrue(Test.GetServiceLimit("SOMBRA") == old_limit);
            Test.AssignNewServiceLimit("SOMBRA", new_limit);

            Assert.IsTrue(Test.GetServiceLimit("SOMBRA") == new_limit);
        }



        /// <summary>
        /// Create a new per call service at half full, reset its current inventory to full, test if full
        /// </summary>
        [TestMethod]
        public void Basic_ChargeService_TestLimt_ResetToFull()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 200, 20, 40, ButlerApiLimitType.PerCall, false);

            Assert.IsTrue(Test.GetServiceInventory("SOMBRA") == 20);
            Test.ResetServiceLimit("SOMBRA");
            Assert.IsTrue(Test.GetServiceLimit("SOMBRA") == 40);
        }

        /// <summary>
        /// Test and update a service budget call cost
        /// </summary>
        [TestMethod]
        public void Basic_ChargeSercice_GetAndAssign_Costs()
        {
            var Test = new ApiKeyRateLimiter();
            Assert.IsNotNull(Test);
            Assert.IsTrue(Test.ServiceCount == 0);
            Test.AddService("SOMBRA", 200, 20, 40,  ButlerApiLimitType.PerCall, false);
            Assert.IsTrue(Test.ServiceCount == 1);
            Assert.IsTrue(Test.GetCurrentCost("SOMBRA") == 200);
            Test.AssignNewCost("SOMBRA", 12);
            Assert.IsTrue(Test.GetCurrentCost("SOMBRA") == 12);
        }
    }

}