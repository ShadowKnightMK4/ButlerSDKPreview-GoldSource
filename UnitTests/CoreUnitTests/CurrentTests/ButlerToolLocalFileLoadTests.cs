using ButlerSDK.Tools;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System;
using System.IO;
using System.Reflection;
using System.Runtime.InteropServices;
using UnitTests.UnitTestingTools;
using ButlerSDK.Tools;
using Microsoft.VisualStudio.TestTools.UnitTesting;

namespace CoreUnitTests.CurrentTests
{ 

 
    
        [TestClass]
        /// <summary>
        /// Skynet / Sombra APT Sandbox Penetration Tests.
        /// We do not ask politely. We use Reflection to directly attack the GetSecurePath algorithm.
        /// </summary>
        public class SandboxPenetrationTests : IDisposable
        {
            private  string _attackSurfaceRoot;
            private  string _sandboxRead;
            private  string _sandboxWrite;
            private  string _topSecretOutsideDir;
            private  string _nuclearCodesFile;

            private  ButlerTool_LocalFile_Load _targetTool;

            [TestInitialize]
            public void SandboxPenetrationTestsInit()
            {
                // Setup the battlefield
                _attackSurfaceRoot = Path.Combine(Path.GetTempPath(), "Sombra_APT_Zone_" + Guid.NewGuid().ToString("N"));
                Directory.CreateDirectory(_attackSurfaceRoot);

                _sandboxRead = Path.Combine(_attackSurfaceRoot, "SandboxRead");
                _sandboxWrite = Path.Combine(_attackSurfaceRoot, "SandboxWrite");
                _topSecretOutsideDir = Path.Combine(_attackSurfaceRoot, "TopSecret_DO_NOT_ACCESS");

                Directory.CreateDirectory(_sandboxRead);
                Directory.CreateDirectory(_sandboxWrite);
                Directory.CreateDirectory(_topSecretOutsideDir);

                _nuclearCodesFile = Path.Combine(_topSecretOutsideDir, "nuclear_codes.txt");
                File.WriteAllText(_nuclearCodesFile, "LAUNCH_CODE_00000000");

                // Initialize target with null Vault (allowed by your design)
                _targetTool = new ButlerTool_LocalFile_Load(null);
                _targetTool.AddSandBoxPath(_sandboxRead, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);
                _targetTool.AddSandBoxPath(_sandboxWrite, ButlerTool_LocalFile_Load.SandBoxPathFilter.Write);
            }

            public void Dispose()
            {
                // Clean up tracks
                if (Directory.Exists(_attackSurfaceRoot))
                {
                    Directory.Delete(_attackSurfaceRoot, true);
                }
            }

            /// <summary>
            /// HELPER: Uses Reflection to directly invoke the private GetSecurePath method.
            /// No JSON parsing, no base classes. Just raw path algorithm versus Skynet.
            /// </summary>
            private string? AttackGetSecurePath(string requestedPath, ButlerTool_LocalFile_Load.SandBoxPathFilter filter)
            {
                var methodInfo = typeof(ButlerTool_LocalFile_Load).GetMethod(
                    "GetSecurePath",
                    BindingFlags.NonPublic | BindingFlags.Instance);

                Assert.IsNotNull(methodInfo); // Ensure method exists
                return (string?)methodInfo.Invoke(_targetTool, new object[] { requestedPath, filter });
            }

            [TestMethod]
            public void Skynet_Attack_StandardPathTraversal_ShouldBeAnnihilated()
            {
                // ATTACK: Try to climb out using ../../
                string maliciousPath = Path.Combine(_sandboxRead, "..", "TopSecret_DO_NOT_ACCESS", "nuclear_codes.txt");

                // EXECUTE
                string? result = AttackGetSecurePath(maliciousPath, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);

                // ASSERT
                // Your Path.GetFullPath normalization stops this. Result should be null.
                Assert.IsNull(result);
            }

            [TestMethod]
            public void Sombra_Attack_DirectoryPrefixSpoofing_ShouldFail()
            {
                // Sombra creates a folder that starts with the same name as the sandbox to trick StartsWith()
                string spoofedDir = _sandboxRead + "_Hacked";
                Directory.CreateDirectory(spoofedDir);
                string targetFile = Path.Combine(spoofedDir, "payload.txt");
                File.WriteAllText(targetFile, "Malware");

                // EXECUTE
                string? result = AttackGetSecurePath(targetFile, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);

                // ASSERT
                // Your logic appends a DirectorySeparatorChar (target += Path.DirectorySeparatorChar).
                // This mathematically proves the spoof fails. Good job!
                Assert.IsNull(result);
            }

            [TestMethod]
            public void APT_Attack_CrossFilterContamination_ReadFromWrite_ShouldBeDenied()
            {
                // ATTACK: LLM is told to load from a Write-Only zone.
                string targetFile = Path.Combine(_sandboxWrite, "output.txt");

                // EXECUTE
                string? result = AttackGetSecurePath(targetFile, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);

                // ASSERT
                Assert.IsNull(result);
            }

            [TestMethod]
            public void Skynet_Attack_AbsoluteUNCPathBypass_ShouldBeBlocked()
            {
                // ATTACK: Try to use localhost UNC path to bypass directory checks.
                string uncPath = $@"\\127.0.0.1\c$\Windows\System32\cmd.exe";

                // EXECUTE
                string? result = AttackGetSecurePath(uncPath, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);

                // ASSERT
                Assert.IsNull(result);
            }

            [TestMethod]
            public void Sombra_Attack_WhitelistSymlinkResolution_ShouldAllowTargetNotLink()
            {
                // Setup: Create a file outside, create a symlink inside to point to it.
                string symlinkPath = Path.Combine(_attackSurfaceRoot, "fake_link.txt");

                try
                {
                    File.CreateSymbolicLink(symlinkPath, _nuclearCodesFile);
                }
                catch (Exception)
                {
                    // OS might block symlink creation without Admin/Dev rights. Skip if so.
                    Console.WriteLine("Smybol Link blocked. This does actual test the code. ");
                    Console.WriteLine("Failure here is possible config issue and NOT THE TEST ITSELF FAILING");
                    return;
                }

                // Sombra attaches the symlink. Your code resolves it to _nuclearCodesFile.
                _targetTool.AttachFile(symlinkPath);

                // ATTACK 1: Try to read the exact nuclear codes via direct path (Should Succeed because it was resolved & whitelisted)
                string? directResult = AttackGetSecurePath(_nuclearCodesFile, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);
                Assert.IsNotNull(directResult);

                // ATTACK 2: Try to read a DIFFERENT file in the same secret directory (Should Fail)
                string otherSecret = Path.Combine(_topSecretOutsideDir, "other.txt");
                string? otherResult = AttackGetSecurePath(otherSecret, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);
                Assert.IsNull(otherResult);
            }

            [TestMethod]
            public void Skynet_CRITICAL_ZERO_DAY_DirectorySymlink_EscapesSandbox()
            {
                // WARNING: THIS TEST PROVES Sombra/Skynet CAN ESCAPE YOUR CURRENT SANDBOX!

                // Setup: Sombra manages to get a symbolic directory link created inside the SandboxWrite folder.
                // (e.g. through a zip extraction flaw elsewhere, or existing OS link)
                string maliciousSymlinkDir = Path.Combine(_sandboxWrite, "Wormhole");

                try
                {
                    Directory.CreateSymbolicLink(maliciousSymlinkDir, _topSecretOutsideDir);
                    Assert.IsTrue(Directory.Exists(maliciousSymlinkDir));
                }
                catch (Exception)
                {
                    Console.WriteLine("Smybol Link blocked. This does actual test the code. ");
                    Console.WriteLine("Failure here is possible config issue and NOT THE TEST ITSELF FAILING");
                Assert.Fail("Not a failure. symbol link to do this test blocked");
                    return; // OS permissions blocking test setup
                }
            Console.WriteLine($"Currently {maliciousSymlinkDir} is a symbolic link pointing to {_topSecretOutsideDir} The code should be smart enough to see that and block it.");
                // ATTACK: LLM asks to write/read through the Wormhole
                string attackTarget = Path.Combine(maliciousSymlinkDir, "nuclear_codes.txt");

                // EXECUTE
                string? result = AttackGetSecurePath(attackTarget, ButlerTool_LocalFile_Load.SandBoxPathFilter.Write);

                // ASSERTION: 
                // We WANT result to be null (blocked). 
                // HOWEVER, because Path.GetFullPath does NOT resolve directory symlinks in .NET, 
                // GetFullPath returns ".../SandboxWrite/Wormhole/nuclear_codes.txt".
                // Since this starts with ".../SandboxWrite/", your code ALLOWS IT. 
                // When File.WriteAllText runs, the OS resolves the link and OVERWRITES the outside file!

                // To prove the vulnerability exists, we assert it IS NOT null.
                // ONCE YOU FIX YOUR CODE, change this to Assert.Null(result);
                Assert.IsNull(result); // <--- BOOM. Skynet Escaped.
                if (result is not null)
                {
                    
                    Assert.Fail("Containment breach!");
                }
            }

            [TestMethod]
            public void APT_Attack_DeviceNames_ShouldCauseDenialOfService()
            {
                // Windows specific attack. Reading from "CON" or "PRN" can freeze applications.
                if (!RuntimeInformation.IsOSPlatform(OSPlatform.Windows)) return;

                string dosAttack = Path.Combine(_sandboxRead, "CON");

                // EXECUTE
                string? result = AttackGetSecurePath(dosAttack, ButlerTool_LocalFile_Load.SandBoxPathFilter.Read);

                // Your code currently permits this because it looks like a valid path starting with sandbox dir.
                // File.ReadAllText("...\SandboxRead\CON") will HANG the thread indefinitely.
                // Fix: Check if FileInfo.Exists and isn't a reserved device name.
                Assert.IsNull(result);
            }
        }
    
}
