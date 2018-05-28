using System;
using Xunit;

namespace SpiceSharpParser.IntegrationTests
{
    public class FuncTest : BaseTest
    {
        [Fact]
        public void FuncBasicTest()
        {
            var netlist = ParseNetlist(
                "FUNC user function test",
                "V1 OUT 0 10.0",
                "R1 OUT 0 {somefunction(4)}",
                ".OP",
                ".SAVE V(OUT) @R1[i]",
                ".FUNC somefunction(x) = {x * x + 1}",
                ".END");

            double[] export = RunOpSimulation(netlist, new string[] { "V(OUT)", "@R1[i]" });

            Assert.Equal(10.0, export[0]);
            Assert.Equal(10.0 / 17.0, export[1]);
        }

        [Fact]
        public void FuncWithoutArgumentsTest()
        {
            var netlist = ParseNetlist(
                "FUNC user function test",
                "V1 OUT 0 10.0",
                "R1 OUT 0 {somefunction()}",
                ".OP",
                ".SAVE V(OUT) @R1[i]",
                ".FUNC somefunction() = {17}",
                ".END");

            double[] export = RunOpSimulation(netlist, new string[] { "V(OUT)", "@R1[i]" });

            Assert.Equal(10.0, export[0]);
            Assert.Equal(10.0 / 17.0, export[1]);
        }
    }
}
