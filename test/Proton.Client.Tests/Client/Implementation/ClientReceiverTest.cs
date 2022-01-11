/*
 * Licensed to the Apache Software Foundation (ASF) under one or more
 * contributor license agreements.  See the NOTICE file distributed with
 * this work for additional information regarding copyright ownership.
 * The ASF licenses this file to You under the Apache License, Version 2.0
 * (the "License"); you may not use this file except in compliance with
 * the License.  You may obtain a copy of the License at
 *
 *      http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Threading;
using Apache.Qpid.Proton.Test.Driver;
using NUnit.Framework;
using Microsoft.Extensions.Logging;
using System;
using Apache.Qpid.Proton.Client.Exceptions;
using Apache.Qpid.Proton.Types.Transport;
using System.Collections.Generic;
using System.Linq;
using Apache.Qpid.Proton.Test.Driver.Matchers;
using System.Threading.Tasks;
using Apache.Qpid.Proton.Utilities;
using Apache.Qpid.Proton.Types.Messaging;

namespace Apache.Qpid.Proton.Client.Implementation
{
   [TestFixture, Timeout(20000)]
   public class ClientReceiverTest : ClientBaseTestFixture
   {
      [Test]
      public void TestCreateReceiverAndClose()
      {
         DoTestCreateReceiverAndCloseOrDetachLink(true);
      }

      [Test]
      public void TestCreateReceiverAndDetach()
      {
         DoTestCreateReceiverAndCloseOrDetachLink(false);
      }

      private void DoTestCreateReceiverAndCloseOrDetachLink(bool close)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().WithSource().WithDistributionMode(Test.Driver.Matchers.Is.NullValue()).And().Respond();
            peer.ExpectFlow().WithLinkCredit(10);
            peer.ExpectDetach().WithClosed(close).Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);

            connection.OpenTask.Wait(TimeSpan.FromSeconds(10));

            ISession session = connection.OpenSession();
            session.OpenTask.Wait(TimeSpan.FromSeconds(10));

            IReceiver receiver = session.OpenReceiver("test-queue");
            receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));

            Assert.AreSame(container, receiver.Client);
            Assert.AreSame(connection, receiver.Connection);
            Assert.AreSame(session, receiver.Session);

            if (close)
            {
               receiver.CloseAsync().Wait(TimeSpan.FromSeconds(10));
            }
            else
            {
               receiver.DetachAsync().Wait(TimeSpan.FromSeconds(10));
            }

            connection.CloseAsync().Wait(TimeSpan.FromSeconds(10));

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCreateReceiverAndCloseSync()
      {
         DoTestCreateReceiverAndCloseOrDetachSync(true);
      }

      [Test]
      public void TestCreateReceiverAndDetachSync()
      {
         DoTestCreateReceiverAndCloseOrDetachSync(false);
      }

      private void DoTestCreateReceiverAndCloseOrDetachSync(bool close)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow().WithLinkCredit(10);
            peer.ExpectDetach().WithClosed(close).Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);

            connection.OpenTask.Wait(TimeSpan.FromSeconds(10));

            ISession session = connection.OpenSession();
            session.OpenTask.Wait(TimeSpan.FromSeconds(10));

            IReceiver receiver = session.OpenReceiver("test-queue");
            receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));

            if (close)
            {
               receiver.Close();
            }
            else
            {
               receiver.Detach();
            }

            connection.CloseAsync().Wait(TimeSpan.FromSeconds(10));

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCreateReceiverAndCloseWithErrorSync()
      {
         DoTestCreateReceiverAndCloseOrDetachWithErrorSync(true);
      }

      [Test]
      public void TestCreateReceiverAndDetachWithErrorSync()
      {
         DoTestCreateReceiverAndCloseOrDetachWithErrorSync(false);
      }

      private void DoTestCreateReceiverAndCloseOrDetachWithErrorSync(bool close)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow();
            peer.ExpectDetach().WithError("amqp-resource-deleted", "an error message").WithClosed(close).Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);

            connection.OpenTask.Wait(TimeSpan.FromSeconds(10));

            ISession session = connection.OpenSession();
            session.OpenTask.Wait(TimeSpan.FromSeconds(10));

            IReceiver receiver = session.OpenReceiver("test-queue");
            receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));

            if (close)
            {
               receiver.Close(IErrorCondition.Create("amqp-resource-deleted", "an error message", null));
            }
            else
            {
               receiver.Detach(IErrorCondition.Create("amqp-resource-deleted", "an error message", null));
            }

            connection.CloseAsync().Wait(TimeSpan.FromSeconds(10));

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverOpenRejectedByRemote()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().Respond().WithNullSource();
            peer.ExpectFlow();
            peer.RemoteDetach().WithErrorCondition(AmqpError.UNAUTHORIZED_ACCESS.ToString(), "Cannot read from this address").Queue();
            peer.ExpectDetach();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-queue");

            try
            {
               receiver.OpenTask.Wait();
               Assert.Fail("Open of receiver should fail due to remote indicating pending close.");
            }
            catch (Exception exe)
            {
               Assert.IsNotNull(exe.InnerException);
               Assert.IsTrue(exe.InnerException is ClientLinkRemotelyClosedException);
               ClientLinkRemotelyClosedException linkClosed = (ClientLinkRemotelyClosedException)exe.InnerException;
               Assert.IsNotNull(linkClosed.Error);
               Assert.AreEqual(AmqpError.UNAUTHORIZED_ACCESS.ToString(), linkClosed.Error.Condition);
            }

            peer.WaitForScriptToComplete();

            // Should not result in any close being sent now, already closed.
            receiver.CloseAsync().Wait();

            peer.ExpectClose().Respond();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete(TimeSpan.FromSeconds(10));
         }
      }

      [Test]
      public void TestOpenReceiverTimesOutWhenNoAttachResponseReceivedTimeout()
      {
         DoTestOpenReceiverTimesOutWhenNoAttachResponseReceived(true);
      }

      [Test]
      public void TestOpenReceiverTimesOutWhenNoAttachResponseReceivedNoTimeout()
      {
         DoTestOpenReceiverTimesOutWhenNoAttachResponseReceived(false);
      }

      private void DoTestOpenReceiverTimesOutWhenNoAttachResponseReceived(bool timeout)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.ExpectDetach();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               OpenTimeout = 10
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options);

            try
            {
               if (timeout)
               {
                  receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));
               }
               else
               {
                  receiver.OpenTask.Wait();
               }

               Assert.Fail("Should not complete the open future without an error");
            }
            catch (Exception exe)
            {
               Exception cause = exe.InnerException;
               Assert.IsTrue(cause is ClientOperationTimedOutException);
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestOpenReceiverWaitWithTimeoutFailsWhenConnectionDrops()
      {
         DoTestOpenReceiverWaitFailsWhenConnectionDrops(true);
      }

      [Test]
      public void TestOpenReceiverWaitWithNoTimeoutFailsWhenConnectionDrops()
      {
         DoTestOpenReceiverWaitFailsWhenConnectionDrops(false);
      }

      private void DoTestOpenReceiverWaitFailsWhenConnectionDrops(bool timeout)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.DropAfterLastHandler(10);
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-queue");

            try
            {
               if (timeout)
               {
                  receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));
               }
               else
               {
                  receiver.OpenTask.Wait();
               }

               Assert.Fail("Should not complete the open future without an error");
            }
            catch (Exception exe)
            {
               Exception cause = exe.InnerException;
               Assert.IsTrue(cause is ClientIOException);
            }

            connection.CloseAsync().GetAwaiter().GetResult();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCloseReceiverTimesOutWhenNoCloseResponseReceivedTimeout()
      {
         DoTestCloseOrDetachReceiverTimesOutWhenNoCloseResponseReceived(true, true);
      }

      [Test]
      public void TestCloseReceiverTimesOutWhenNoCloseResponseReceivedNoTimeout()
      {
         DoTestCloseOrDetachReceiverTimesOutWhenNoCloseResponseReceived(true, false);
      }

      [Test]
      public void TestDetachReceiverTimesOutWhenNoCloseResponseReceivedTimeout()
      {
         DoTestCloseOrDetachReceiverTimesOutWhenNoCloseResponseReceived(false, true);
      }

      [Test]
      public void TestDetachReceiverTimesOutWhenNoCloseResponseReceivedNoTimeout()
      {
         DoTestCloseOrDetachReceiverTimesOutWhenNoCloseResponseReceived(false, false);
      }

      private void DoTestCloseOrDetachReceiverTimesOutWhenNoCloseResponseReceived(bool close, bool timeout)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow();
            peer.ExpectDetach();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               CloseTimeout = 5
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            connection.OpenTask.Wait(TimeSpan.FromSeconds(10));

            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-queue");
            receiver.OpenTask.Wait(TimeSpan.FromSeconds(10));

            try
            {
               if (close)
               {
                  if (timeout)
                  {
                     receiver.CloseAsync().Wait(TimeSpan.FromSeconds(10));
                  }
                  else
                  {
                     receiver.CloseAsync().Wait();
                  }
               }
               else
               {
                  if (timeout)
                  {
                     receiver.DetachAsync().Wait(TimeSpan.FromSeconds(10));
                  }
                  else
                  {
                     receiver.DetachAsync().Wait();
                  }
               }

               Assert.Fail("Should not complete the close or detach future without an error");
            }
            catch (Exception exe)
            {
               Exception cause = exe.InnerException;
               Assert.IsTrue(cause is ClientOperationTimedOutException);
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverDrainAllOutstanding()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               CreditWindow = 0
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options).OpenTask.Result;

            peer.WaitForScriptToComplete();

            // Add some credit, verify not draining
            uint credit = 7;
            peer.ExpectFlow().WithDrain(Matches.AnyOf(Test.Driver.Matchers.Is.EqualTo(false),
                                                      Test.Driver.Matchers.Is.NullValue()))
                             .WithLinkCredit(credit).WithDeliveryCount(0);

            receiver.AddCredit(credit);

            peer.WaitForScriptToComplete();

            // Drain all the credit
            peer.ExpectFlow().WithDrain(true).WithLinkCredit(credit).WithDeliveryCount(0)
                             .Respond()
                             .WithDrain(true).WithLinkCredit(0).WithDeliveryCount(credit);

            Task<IReceiver> draining = receiver.Drain();
            draining.Wait(TimeSpan.FromSeconds(5));

            // Close things down
            peer.ExpectClose().Respond();
            connection.Close();

            peer.WaitForScriptToComplete(TimeSpan.FromSeconds(10));
         }
      }

      [Test]
      public void TestDrainCompletesWhenReceiverHasNoCredit()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               CreditWindow = 0
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options).OpenTask.Result;

            peer.WaitForScriptToComplete();

            Task<IReceiver> draining = receiver.Drain();
            draining.Wait(TimeSpan.FromSeconds(5));

            // Close things down
            peer.ExpectClose().Respond();
            connection.Close();

            peer.WaitForScriptToComplete(TimeSpan.FromSeconds(10));
         }
      }

      [Test]
      public void TestDrainAdditionalDrainCallThrowsWhenReceiverStillDraining()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow();
            peer.ExpectFlow().WithDrain(true);
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-queue").OpenTask.Result;

            receiver.Drain();

            try
            {
               receiver.Drain().Wait();
               Assert.Fail("Drain call should fail since already draining.");
            }
            catch (Exception cliEx)
            {
               logger.LogDebug("Receiver threw error on drain call", cliEx);
               Assert.IsTrue(cliEx.InnerException is ClientIllegalStateException);
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestAddCreditFailsWhileDrainPending()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond().WithInitialDeliveryCount(20);
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               CreditWindow = 0
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options).OpenTask.Result;

            peer.WaitForScriptToComplete();

            // Add some credit, verify not draining
            uint credit = 7;
            peer.ExpectFlow().WithDrain(Matches.AnyOf(Test.Driver.Matchers.Is.EqualTo(false),
                                                      Test.Driver.Matchers.Is.NullValue()))
                             .WithLinkCredit(credit);

            // Ensure we get the attach response with the initial delivery count.
            receiver.OpenTask.Result.AddCredit(credit);

            peer.WaitForScriptToComplete();

            // Drain all the credit
            peer.ExpectFlow().WithDrain(true).WithLinkCredit(credit).WithDeliveryCount(20);
            peer.ExpectClose().Respond();

            Task<IReceiver> draining = receiver.Drain();
            Assert.IsFalse(draining.IsCompleted);

            try
            {
               receiver.AddCredit(1);
               Assert.Fail("Should not allow add credit when drain is pending");
            }
            catch (ClientIllegalStateException)
            {
               // Expected
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestAddCreditFailsWhenCreditWindowEnabled()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow().WithLinkCredit(10);
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               CreditWindow = 10 // Explicitly set a credit window to unsure behavior is consistent.
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options).OpenTask.Result;

            peer.WaitForScriptToComplete();
            peer.ExpectClose().Respond();

            try
            {
               receiver.AddCredit(1);
               Assert.Fail("Should not allow add credit when credit window configured");
            }
            catch (ClientIllegalStateException)
            {
               // Expected
            }

            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCreateDynamicReceiver()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSource().WithDynamic(true).WithAddress((string)null)
                               .And().Respond()
                               .WithSource().WithDynamic(true).WithAddress("test-dynamic-node");
            peer.ExpectFlow();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenDynamicReceiver().OpenTask.Result;

            Assert.IsNotNull(receiver.Address, "Remote should have assigned the address for the dynamic receiver");
            Assert.AreEqual("test-dynamic-node", receiver.Address);

            receiver.Close();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Ignore("Test peer matching on dictionary equality not worked out yet")]
      [Test]
      public void TestCreateDynamicReceiverWthNodeProperties()
      {
         IDictionary<string, object> nodeProperties = new Dictionary<string, object>();
         nodeProperties.Add("test-property-1", "one");
         nodeProperties.Add("test-property-2", "two");
         nodeProperties.Add("test-property-3", "three");

         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSource()
                                   .WithDynamic(true)
                                   .WithAddress((String)null)
                                   .WithDynamicNodeProperties(nodeProperties)
                               .And().Respond()
                               .WithSource()
                                   .WithDynamic(true)
                                   .WithAddress("test-dynamic-node")
                                   .WithDynamicNodeProperties(nodeProperties);
            peer.ExpectFlow();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenDynamicReceiver(null, nodeProperties).OpenTask.Result;

            Assert.IsNotNull(receiver.Address, "Remote should have assigned the address for the dynamic receiver");
            Assert.AreEqual("test-dynamic-node", receiver.Address);

            receiver.Close();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCreateDynamicReceiverWithNoCreditWindow()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSource().WithDynamic(true).WithAddress((String)null)
                               .And().Respond()
                               .WithSource().WithDynamic(true).WithAddress("test-dynamic-node");
            peer.ExpectAttach().OfSender().Respond();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions receiverOptions = new ReceiverOptions()
            {
               CreditWindow = 0
            };
            IReceiver receiver = session.OpenDynamicReceiver(receiverOptions).OpenTask.Result;

            // Perform another round trip operation to ensure we see that no flow frame was
            // sent by the receiver
            session.OpenSender("test");

            Assert.IsNotNull(receiver.Address, "Remote should have assigned the address for the dynamic receiver");
            Assert.AreEqual("test-dynamic-node", receiver.Address);

            receiver.Close();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestDynamicReceiverAddressWaitsForRemoteAttach()
      {
         tryReadDynamicReceiverAddress(true);
      }

      [Test]
      public void TestDynamicReceiverAddressFailsAfterOpenTimeout()
      {
         tryReadDynamicReceiverAddress(false);
      }

      private void tryReadDynamicReceiverAddress(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSource().WithDynamic(true).WithAddress((String)null);
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               OpenTimeout = 100
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenDynamicReceiver();

            peer.WaitForScriptToComplete();

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().WithSource().WithAddress("test-dynamic-node").And().Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.Address, "Remote should have assigned the address for the dynamic receiver");
               Assert.AreEqual("test-dynamic-node", receiver.Address);
            }
            else
            {
               try
               {
                  _ = receiver.Address;
                  Assert.Fail("Should failed to get address due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from address call", ex);
               }
            }

            try
            {
               receiver.Close();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCreateReceiverWithQoSOfAtMostOnce()
      {
         DoTestCreateReceiverWithConfiguredQoS(DeliveryMode.AtMostOnce);
      }

      [Test]
      public void TestCreateReceiverWithQoSOfAtLeastOnce()
      {
         DoTestCreateReceiverWithConfiguredQoS(DeliveryMode.AtLeastOnce);
      }

      private void DoTestCreateReceiverWithConfiguredQoS(DeliveryMode qos)
      {
         byte sndMode = (byte)(qos == DeliveryMode.AtMostOnce ? SenderSettleMode.Settled : SenderSettleMode.Unsettled);

         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSndSettleMode(sndMode)
                               .WithRcvSettleMode((byte)ReceiverSettleMode.First)
                               .Respond()
                               .WithSndSettleMode(sndMode)
                               .WithRcvSettleMode((byte?)ReceiverSettleMode.First);
            peer.ExpectFlow();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               DeliveryMode = qos
            };
            IReceiver receiver = session.OpenReceiver("test-qos", options).OpenTask.Result;

            Assert.AreEqual("test-qos", receiver.Address);

            receiver.Close();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverGetSourceWaitsForRemoteAttach()
      {
         TryReadReceiverSource(true);
      }

      [Test]
      public void TestReceiverGetSourceFailsAfterOpenTimeout()
      {
         TryReadReceiverSource(false);
      }

      private void TryReadReceiverSource(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               OpenTimeout = 100
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;

            IReceiver receiver = session.OpenReceiver("test-receiver");

            peer.WaitForScriptToComplete();

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.Source, "Remote should have responded with a Source value");
               Assert.AreEqual("test-receiver", receiver.Source.Address);
            }
            else
            {
               try
               {
                  _ = receiver.Source;
                  Assert.Fail("Should failed to get remote source due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from blocking call", ex);
               }
            }

            try
            {
               receiver.CloseAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverGetTargetWaitsForRemoteAttach()
      {
         TryReadReceiverTarget(true);
      }

      [Test]
      public void TestReceiverGetTargetFailsAfterOpenTimeout()
      {
         TryReadReceiverTarget(false);
      }

      private void TryReadReceiverTarget(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               OpenTimeout = 100
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-receiver");

            peer.WaitForScriptToComplete();

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.Target, "Remote should have responded with a Target value");
            }
            else
            {
               try
               {
                  _ = receiver.Target;
                  Assert.Fail("Should failed to get remote source due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from blocking call", ex);
               }
            }

            try
            {
               receiver.CloseAsync().Wait();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverGetRemotePropertiesWaitsForRemoteAttach()
      {
         TryReadReceiverRemoteProperties(true);
      }

      [Test]
      public void TestReceiverGetRemotePropertiesFailsAfterOpenTimeout()
      {
         TryReadReceiverRemoteProperties(false);
      }

      private void TryReadReceiverRemoteProperties(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               OpenTimeout = 100
            };
            IReceiver receiver = session.OpenReceiver("test-receiver", options);

            peer.WaitForScriptToComplete();

            IDictionary<string, object> expectedProperties = new Dictionary<string, object>();
            expectedProperties.Add("TEST", "test-property");

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().WithPropertiesMap(expectedProperties).Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.Properties, "Remote should have responded with a remote properties value");
               Assert.AreEqual(expectedProperties, receiver.Properties);
            }
            else
            {
               try
               {
                  _ = receiver.Properties;
                  Assert.Fail("Should failed to get remote state due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from blocking call", ex);
               }
            }

            try
            {
               receiver.Close();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverGetRemoteOfferedCapabilitiesWaitsForRemoteAttach()
      {
         TryReadReceiverRemoteOfferedCapabilities(true);
      }

      [Test]
      public void TestReceiverGetRemoteOfferedCapabilitiesFailsAfterOpenTimeout()
      {
         TryReadReceiverRemoteOfferedCapabilities(false);
      }

      private void TryReadReceiverRemoteOfferedCapabilities(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               OpenTimeout = 100
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-receiver");

            peer.WaitForScriptToComplete();

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().WithOfferedCapabilities("QUEUE").Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.OfferedCapabilities, "Remote should have responded with a remote offered Capabilities value");
               Assert.AreEqual(1, receiver.OfferedCapabilities.Count);
               Assert.AreEqual("QUEUE", receiver.OfferedCapabilities.ElementAt(0));
            }
            else
            {
               try
               {
                  _ = receiver.OfferedCapabilities;
                  Assert.Fail("Should failed to get remote state due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from blocking call", ex);
               }
            }

            try
            {
               receiver.Close();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestReceiverGetRemoteDesiredCapabilitiesWaitsForRemoteAttach()
      {
         TryReadReceiverRemoteDesiredCapabilities(true);
      }

      [Test]
      public void TestReceiverGetRemoteDesiredCapabilitiesFailsAfterOpenTimeout()
      {
         TryReadReceiverRemoteDesiredCapabilities(false);
      }

      private void TryReadReceiverRemoteDesiredCapabilities(bool attachResponse)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            ConnectionOptions options = new ConnectionOptions()
            {
               OpenTimeout = 100
            };
            IConnection connection = container.Connect(remoteAddress, remotePort, options).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            IReceiver receiver = session.OpenReceiver("test-receiver");

            peer.WaitForScriptToComplete();

            if (attachResponse)
            {
               peer.ExpectDetach().Respond();
               peer.RespondToLastAttach().WithDesiredCapabilities("Error-Free").Later(10);
            }
            else
            {
               peer.ExpectDetach();
            }

            if (attachResponse)
            {
               Assert.IsNotNull(receiver.DesiredCapabilities, "Remote should have responded with a remote desired Capabilities value");
               Assert.AreEqual(1, receiver.DesiredCapabilities.Count);
               Assert.AreEqual("Error-Free", receiver.DesiredCapabilities.ElementAt(0));
            }
            else
            {
               try
               {
                  _ = receiver.DesiredCapabilities;
                  Assert.Fail("Should failed to get remote state due to no attach response");
               }
               catch (ClientException ex)
               {
                  logger.LogDebug("Caught expected exception from blocking call", ex);
               }
            }

            try
            {
               receiver.CloseAsync().GetAwaiter().GetResult();
            }
            catch (Exception ex)
            {
               logger.LogDebug("Caught unexpected exception from close call", ex);
               Assert.Fail("Should not fail to close when connection not closed and detach sent");
            }

            peer.ExpectClose().Respond();
            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestBlockingReceiveCancelledWhenReceiverClosed()
      {
         DoTestBlockingReceiveCancelledWhenReceiverClosedOrDetached(true);
      }

      [Test]
      public void TestBlockingReceiveCancelledWhenReceiverDetached()
      {
         DoTestBlockingReceiveCancelledWhenReceiverClosedOrDetached(false);
      }

      public void DoTestBlockingReceiveCancelledWhenReceiverClosedOrDetached(bool close)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort).OpenTask.Result;
            ISession session = connection.OpenSession().OpenTask.Result;
            ReceiverOptions options = new ReceiverOptions()
            {
               CreditWindow = 0
            };
            IReceiver receiver = session.OpenReceiver("test-queue", options).OpenTask.Result;

            peer.WaitForScriptToComplete();
            peer.ExpectFlow().WithLinkCredit(10);
            peer.Execute(() =>
            {
               if (close)
               {
                  receiver.CloseAsync();
               }
               else
               {
                  receiver.DetachAsync();
               }
            }).Queue();
            peer.ExpectDetach().WithClosed(close).Respond();
            peer.ExpectClose().Respond();

            receiver.AddCredit(10);

            try
            {
               receiver.Receive();
               Assert.Fail("Should throw to indicate that receiver was closed");
            }
            catch (ClientException)
            {
            }

            connection.Close();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestBlockingReceiveCancelledWhenReceiverRemotelyClosed()
      {
         DoTestBlockingReceiveCancelledWhenReceiverRemotelyClosedOrDetached(true);
      }

      [Test]
      public void TestBlockingReceiveCancelledWhenReceiverRemotelyDetached()
      {
         DoTestBlockingReceiveCancelledWhenReceiverRemotelyClosedOrDetached(false);
      }

      public void DoTestBlockingReceiveCancelledWhenReceiverRemotelyClosedOrDetached(bool close)
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow().WithLinkCredit(10);
            peer.RemoteDetach().WithClosed(close)
                               .WithErrorCondition(AmqpError.RESOURCE_DELETED.ToString(), "Address was manually deleted")
                               .AfterDelay(10).Queue();
            peer.ExpectDetach().WithClosed(close);
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);
            ISession session = connection.OpenSession();
            IReceiver receiver = session.OpenReceiver("test-queue").OpenTask.Result;

            try
            {
               receiver.Receive();
               Assert.Fail("Client should throw to indicate remote closed the receiver forcibly.");
            }
            catch (ClientIllegalStateException)
            {
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestCloseReceiverWithErrorCondition()
      {
         DoTestCloseOrDetachWithErrorCondition(true);
      }

      [Test]
      public void TestDetachReceiverWithErrorCondition()
      {
         DoTestCloseOrDetachWithErrorCondition(false);
      }

      public void DoTestCloseOrDetachWithErrorCondition(bool close)
      {
         string condition = "amqp:link:detach-forced";
         string description = "something bad happened.";

         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow();
            peer.ExpectDetach().WithClosed(close).WithError(condition, description).Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);
            ISession session = connection.OpenSession();
            IReceiver receiver = session.OpenReceiver("test-queue").OpenTask.Result;

            if (close)
            {
               receiver.CloseAsync(IErrorCondition.Create(condition, description, null));
            }
            else
            {
               receiver.DetachAsync(IErrorCondition.Create(condition, description, null));
            }

            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }

      [Test]
      public void TestOpenReceiverWithLinCapabilities()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver()
                               .WithSource().WithCapabilities("queue").And()
                               .Respond();
            peer.ExpectFlow();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);
            ISession session = connection.OpenSession();
            ReceiverOptions receiverOptions = new ReceiverOptions();
            receiverOptions.SourceOptions.Capabilities = new string[] { "queue" };
            IReceiver receiver = session.OpenReceiver("test-queue", receiverOptions);

            receiver.OpenTask.Wait();

            receiver.Close();

            connection.CloseAsync().Wait(TimeSpan.FromSeconds(10));

            peer.WaitForScriptToComplete();
         }
      }

      [Ignore("Message processing for decode etc is not yet implemented")]
      [Test]
      public void TestReceiveMessageInSplitTransferFrames()
      {
         using (ProtonTestServer peer = new ProtonTestServer(loggerFactory))
         {
            peer.ExpectSASLAnonymousConnect();
            peer.ExpectOpen().Respond();
            peer.ExpectBegin().Respond();
            peer.ExpectAttach().OfReceiver().Respond();
            peer.ExpectFlow();
            peer.Start();

            string remoteAddress = peer.ServerAddress;
            int remotePort = peer.ServerPort;

            logger.LogInformation("Test started, peer listening on: {0}:{1}", remoteAddress, remotePort);

            IClient container = IClient.Create();
            IConnection connection = container.Connect(remoteAddress, remotePort);
            ISession session = connection.OpenSession();
            IReceiver receiver = session.OpenReceiver("test-queue").OpenTask.Result;

            byte[] payload = CreateEncodedMessage(new AmqpValue("Hello World"));

            byte[] slice1 = Statics.CopyOfRange(payload, 0, 2);
            byte[] slice2 = Statics.CopyOfRange(payload, 2, 4);
            byte[] slice3 = Statics.CopyOfRange(payload, 4, payload.Length);

            peer.RemoteTransfer().WithHandle(0)
                                 .WithDeliveryId(0)
                                 .WithDeliveryTag(new byte[] { 1 })
                                 .WithMore(true)
                                 .WithMessageFormat(0)
                                 .WithPayload(slice1).Now();

            Assert.IsNull(receiver.TryReceive());

            peer.RemoteTransfer().WithHandle(0)
                                 .WithMore(true)
                                 .WithMessageFormat(0)
                                 .WithPayload(slice2).Now();

            Assert.IsNull(receiver.TryReceive());

            peer.RemoteTransfer().WithHandle(0)
                                 .WithMore(false)
                                 .WithMessageFormat(0)
                                 .WithPayload(slice3).Now();

            peer.ExpectDisposition().WithSettled(true).WithState().Accepted();
            peer.ExpectDetach().Respond();
            peer.ExpectClose().Respond();

            IDelivery delivery = receiver.Receive();
            Assert.IsNotNull(delivery);
            IMessage<object> received = delivery.Message();
            Assert.IsNotNull(received);
            Assert.IsTrue(received.Body is String);
            String value = (String)received.Body;
            Assert.AreEqual("Hello World", value);

            delivery.Accept();
            receiver.CloseAsync();
            connection.CloseAsync().Wait();

            peer.WaitForScriptToComplete();
         }
      }
   }
}