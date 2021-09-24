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

using System;
using System.Collections.Generic;
using System.IO;
using Apache.Qpid.Proton.Buffer;
using Apache.Qpid.Proton.Codec;
using Apache.Qpid.Proton.Test.Driver;
using NUnit.Framework;

namespace Apache.Qpid.Proton.Engine.Implementation
{
   public abstract class ProtonEngineTestSupport
   {
      protected IList<IProtonBuffer> engineWrites = new List<IProtonBuffer>();

      protected Exception failure;
      protected string name;

      protected readonly IDecoder decoder = CodecFactory.DefaultDecoder;
      protected IDecoderState decoderState;

      protected readonly IEncoder encoder = CodecFactory.DefaultEncoder;
      protected IEncoderState encoderState;

      [OneTimeSetUp]
      public void OneTimeSetup()
      {
         decoderState = decoder.NewDecoderState();
         encoderState = encoder.NewEncoderState();
      }

      [SetUp]
      public void SetUp()
      {
         failure = null;
         name = TestContext.CurrentContext.Test.Name;
         decoderState.Reset();
         encoderState.Reset();
      }

      [TearDown]
      public void TearDown()
      {

      }

      protected ProtonTestConnector CreateTestPeer(IEngine engine)
      {
         ProtonTestConnector peer = new ProtonTestConnector(stream =>
         {
            if (stream is MemoryStream mem)
            {
               engine.Ingest(ProtonByteBufferAllocator.Instance.Wrap(mem.ToArray()));
            }
            else
            {
               byte[] bytes = new byte[stream.Length - stream.Position];
               stream.Read(bytes, 0, bytes.Length);
               engine.Ingest(ProtonByteBufferAllocator.Instance.Wrap(bytes));
            }
         });

         engine.OutputHandler(buffer =>
         {
            peer.Ingest(new ProtonBufferInputStream(buffer));
         });

         return peer;
      }

      protected ProtonTestConnector CreateTestPeer(IEngine engine, Queue<Action> asyncIOCallback)
      {
         ProtonTestConnector peer = new ProtonTestConnector(stream =>
         {
            if (stream is MemoryStream mem)
            {
               engine.Ingest(ProtonByteBufferAllocator.Instance.Wrap(mem.ToArray()));
            }
            else
            {
               byte[] bytes = new byte[stream.Length - stream.Position];
               stream.Read(bytes, 0, bytes.Length);
               engine.Ingest(ProtonByteBufferAllocator.Instance.Wrap(bytes));
            }
         });

         engine.OutputHandler((buffer, asyncCallback) =>
         {
            if (asyncCallback != null)
            {
               asyncIOCallback.Enqueue(asyncCallback);
            }
            peer.Ingest(new ProtonBufferInputStream(buffer));
         });

         return peer;
      }

      protected uint CountElements<T>(IEnumerable<T> enumerable)
      {
         uint count = 0;

         foreach (T element in enumerable)
         {
            count++;
         }

         return count;
      }
   }
}