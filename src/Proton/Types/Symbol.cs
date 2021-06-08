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

namespace Apache.Qpid.Proton.Types
{
   public sealed class Symbol
   {
      /// <summary>
      /// Lookup or create a singleton instance of the given Symbol that has the
      /// matching name to the string value provided.
      /// </summary>
      /// <param name="value">the stringified symbol name</param>
      /// <returns>A singleton instance of the named Symbol</returns>
      public static Symbol Lookup(string value)
      {
         return null;
      }
   }
}