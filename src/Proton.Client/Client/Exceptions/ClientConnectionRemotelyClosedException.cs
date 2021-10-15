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

namespace Apache.Qpid.Proton.Client.Exceptions
{
   /// <summary>
   /// Exception thrown when the remote peer actively closes the connection} by sending
   /// and AMQP Close frame or when the IO layer is disconnected due to some other
   /// reason such as a security error or transient network error.
   /// </summary>
   public class ClientConnectionRemotelyClosedException : ClientIOException
   {
      private readonly IErrorCondition errorCondition;

      /// <summary>
      /// Creates an instance of this exception with the given message
      /// </summary>
      /// <param name="message">The message that describes the error</param>
      public ClientConnectionRemotelyClosedException(string message) : base(message)
      {
      }

      /// <summary>
      /// Creates an instance of this exception with the given message and
      /// linked causal exception.
      /// </summary>
      /// <param name="message">The message that describes the error</param>
      /// <param name="innerException">The exception that caused this error</param>
      public ClientConnectionRemotelyClosedException(string message, Exception innerException) : base(message, innerException)
      {
      }

      /// <summary>
      /// Creates an instance of this exception with the given message
      /// </summary>
      /// <param name="message">The message that describes the error</param>
      public ClientConnectionRemotelyClosedException(string message, IErrorCondition errorCondition) : base(message)
      {
         this.errorCondition = errorCondition;
      }

      /// <summary>
      /// Creates an instance of this exception with the given message and
      /// linked causal exception.
      /// </summary>
      /// <param name="message">The message that describes the error</param>
      /// <param name="innerException">The exception that caused this error</param>
      public ClientConnectionRemotelyClosedException(string message, Exception innerException, IErrorCondition errorCondition) : base(message, innerException)
      {
         this.errorCondition = errorCondition;
      }

      /// <summary>
      /// Return the error provided by the remote if any.
      /// </summary>
      public IErrorCondition Error => errorCondition;
   }
}