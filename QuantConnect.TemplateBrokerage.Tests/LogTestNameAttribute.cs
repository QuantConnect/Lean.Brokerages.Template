/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using NUnit.Framework;
using QuantConnect.Logging;
using NUnit.Framework.Interfaces;

[assembly: QuantConnect.Brokerages.Template.Tests.LogTestName]

namespace QuantConnect.Brokerages.Template.Tests
{
    /// <summary>
    /// Writes the name of a test to the log when it starts and when it ends. The line above applies it to the whole
    /// assembly, so every test is framed in the log without adding anything to the test itself.
    /// </summary>
    [AttributeUsage(AttributeTargets.Assembly)]
    public sealed class LogTestNameAttribute : Attribute, ITestAction
    {
        /// <summary>
        /// The line written above and below the name of a test, so the run of one test can be read apart from the next.
        /// </summary>
        private const string TestSeparator = "============================================================";

        /// <summary>
        /// Runs around every single test instead of once around the fixture.
        /// </summary>
        public ActionTargets Targets => ActionTargets.Test;

        /// <summary>
        /// Writes the name of the test that is about to run.
        /// </summary>
        /// <param name="test">The test that is about to run.</param>
        public void BeforeTest(ITest test)
        {
            Log.Trace(TestSeparator);
            Log.Trace($"==== TEST START: {test.Name}");
            Log.Trace(TestSeparator);
        }

        /// <summary>
        /// Writes the name of the test that finished together with the result it ended on.
        /// </summary>
        /// <param name="test">The test that finished.</param>
        public void AfterTest(ITest test)
        {
            Log.Trace(TestSeparator);
            Log.Trace($"==== TEST END: {test.Name} - {TestContext.CurrentContext.Result.Outcome.Status}");
            Log.Trace(TestSeparator);
        }
    }
}
