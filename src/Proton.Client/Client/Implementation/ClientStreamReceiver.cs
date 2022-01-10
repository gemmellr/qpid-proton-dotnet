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
using System.Threading.Tasks;
using Apache.Qpid.Proton.Client.Exceptions;
using Apache.Qpid.Proton.Client.Concurrent;
using Apache.Qpid.Proton.Engine;
using Apache.Qpid.Proton.Logging;

namespace Apache.Qpid.Proton.Client.Implementation
{
   /// <summary>
   /// Implements the streaming message receiver which allows for reading of large
   /// messages in smaller chunks. The API allows for multiple calls to receiver but
   /// any call that happens after a large message receives begins will be blocked
   /// until the previous large messsage is fully read and the next arrives.
   /// </summary>
   public sealed class ClientStreamReceiver : IStreamReceiver
   {
      private static IProtonLogger LOG = ProtonLoggerFactory.GetLogger<ClientStreamReceiver>();

      private readonly StreamReceiverOptions options;
      private readonly ClientSession session;
      private readonly string receiverId;
      private readonly AtomicBoolean closed = new AtomicBoolean();
      private readonly TaskCompletionSource<IReceiver> openFuture = new TaskCompletionSource<IReceiver>();
      private readonly TaskCompletionSource<IStreamReceiver> closeFuture = new TaskCompletionSource<IStreamReceiver>();

      private TaskCompletionSource<IReceiver> drainingFuture;

      private Engine.IReceiver protonReceiver;
      private ClientException failureCause;
      private volatile ISource remoteSource;
      private volatile ITarget remoteTarget;

      internal ClientStreamReceiver(ClientSession session, StreamReceiverOptions options, string receiverId, Engine.IReceiver receiver)
      {
         this.options = options;
         this.session = session;
         this.receiverId = receiverId;
         this.protonReceiver = receiver;
         this.protonReceiver.LinkedResource = this;

         if (options.CreditWindow > 0)
         {
            protonReceiver.AddCredit(options.CreditWindow);
         }
      }

      public IClient Client => session.Client;

      public IConnection Connection => session.Connection;

      public ISession Session => session;

      public Task<IReceiver> OpenTask => openFuture.Task;

      public string Address
      {
         get
         {
            if (IsDynamic)
            {
               WaitForOpenToComplete();
               return protonReceiver.RemoteSource?.Address;
            }
            else
            {
               return protonReceiver.Source?.Address;
            }
         }
      }

      public ISource Source
      {
         get
         {
            WaitForOpenToComplete();
            return remoteSource;
         }
      }

      public ITarget Target
      {
         get
         {
            WaitForOpenToComplete();
            return remoteTarget;
         }
      }

      public IReadOnlyDictionary<string, object> Properties
      {
         get
         {
            WaitForOpenToComplete();
            return ClientConversionSupport.ToStringKeyedMap(protonReceiver.RemoteProperties);
         }
      }

      public IReadOnlyCollection<string> OfferedCapabilities
      {
         get
         {
            WaitForOpenToComplete();
            return ClientConversionSupport.ToStringArray(protonReceiver.OfferedCapabilities);
         }
      }

      public IReadOnlyCollection<string> DesiredCapabilities
      {
         get
         {
            WaitForOpenToComplete();
            return ClientConversionSupport.ToStringArray(protonReceiver.DesiredCapabilities);
         }
      }

      public int QueuedDeliveries
      {
         get
         {
            WaitForOpenToComplete();
            throw new NotImplementedException();
         }
      }

      public IStreamReceiver AddCredit(uint credit)
      {
         CheckClosedOrFailed();
         TaskCompletionSource<IStreamReceiver> creditAdded = new TaskCompletionSource<IStreamReceiver>();

         session.Execute(() =>
         {
            if (NotClosedOrFailed(creditAdded))
            {
               if (options.CreditWindow != 0)
               {
                  creditAdded.TrySetException(new ClientIllegalStateException("Cannot add credit when a credit window has been configured"));
               }
               else if (protonReceiver.IsDraining)
               {
                  creditAdded.TrySetException(new ClientIllegalStateException("Cannot add credit while a drain is pending"));
               }
               else
               {
                  try
                  {
                     protonReceiver.AddCredit(credit);
                     creditAdded.TrySetResult(this);
                  }
                  catch (Exception ex)
                  {
                     creditAdded.TrySetException(ClientExceptionSupport.CreateNonFatalOrPassthrough(ex));
                  }
               }
            }
         });

         return session.Request(this, creditAdded).Task.GetAwaiter().GetResult();
      }

      public Task<IReceiver> Drain()
      {
         CheckClosedOrFailed();
         TaskCompletionSource<IReceiver> drainComplete = new TaskCompletionSource<IReceiver>();

         session.Execute(() =>
         {
            if (NotClosedOrFailed(drainComplete))
            {
               if (protonReceiver.IsDraining)
               {
                  drainComplete.TrySetException(new ClientIllegalStateException("Stream Receiver is already draining"));
                  return;
               }

               try
               {
                  if (protonReceiver.Drain())
                  {
                     drainingFuture = drainComplete;
                     // TODO: Need a cancellation point: drainingTimeout
                     session.ScheduleRequestTimeout(drainingFuture, options.DrainTimeout,
                         () => new ClientOperationTimedOutException("Timed out waiting for remote to respond to drain request"));
                  }
                  else
                  {
                     drainComplete.TrySetResult(this);
                  }
               }
               catch (Exception ex)
               {
                  drainComplete.TrySetException(ClientExceptionSupport.CreateNonFatalOrPassthrough(ex));
               }
            }
         });

         return drainComplete.Task;
      }

      public void Close(IErrorCondition error = null)
      {
         throw new NotImplementedException();
      }

      public Task<IReceiver> CloseAsync(IErrorCondition error = null)
      {
         throw new NotImplementedException();
      }

      public void Detach(IErrorCondition error = null)
      {
         throw new NotImplementedException();
      }

      public Task<IReceiver> DetachAsync(IErrorCondition error = null)
      {
         throw new NotImplementedException();
      }

      public void Dispose()
      {
         throw new NotImplementedException();
      }

      public IStreamDelivery Receive()
      {
         throw new NotImplementedException();
      }

      public IStreamDelivery Receive(TimeSpan timeout)
      {
         throw new NotImplementedException();
      }

      public IStreamDelivery TryReceive()
      {
         throw new NotImplementedException();
      }

      #region Internal Receiver API

      internal ClientStreamReceiver Open()
      {
         // TODO

         return this;
      }

      internal void Disposition(IIncomingDelivery delivery, Types.Transport.IDeliveryState state, bool settle)
      {
         // TODO CheckClosedOrFailed();
         // asyncApplyDisposition(delivery, state, settle);
      }

      internal String ReceiverId => receiverId;

      internal bool IsClosed => closed;

      internal bool IsDynamic => protonReceiver.Source?.Dynamic ?? false;

      internal Exception FailureCause => failureCause;

      internal StreamReceiverOptions ReceiverOptions => options;

      #endregion

      #region Private Receiver Implementation

      private void WaitForOpenToComplete()
      {
         if (!openFuture.Task.IsCompleted || openFuture.Task.IsFaulted)
         {
            try
            {
               openFuture.Task.Wait();
            }
            catch (Exception e)
            {
               throw failureCause ?? ClientExceptionSupport.CreateNonFatalOrPassthrough(e);
            }
         }
      }

      private void CheckClosedOrFailed()
      {
         if (IsClosed)
         {
            throw new ClientIllegalStateException("The StreamReceiver was explicitly closed", failureCause);
         }
         else if (failureCause != null)
         {
            throw failureCause;
         }
      }

      private bool NotClosedOrFailed<T>(TaskCompletionSource<T> request)
      {
         if (IsClosed)
         {
            request.TrySetException(new ClientIllegalStateException("The Receiver was explicitly closed", failureCause));
            return false;
         }
         else if (failureCause != null)
         {
            request.TrySetException(failureCause);
            return false;
         }
         else
         {
            return true;
         }
      }

      #endregion
   }
}