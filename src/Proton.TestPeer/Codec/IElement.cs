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
using System.Text;

namespace Apache.Qpid.Proton.Test.Driver.Codec
{
   public interface IElement
   {
      int Size { get; }

      object Value { get; }

      DataType DataType { get; }

      int Encode(Span<byte> buffer);

      IElement Next { get; set; }

      IElement Prev { get; set; }

      IElement Child { get; set; }

      IElement Parent { get; set; }

      IElement ReplaceWith(IElement elt);

      IElement AddChild(IElement element);

      IElement CheckChild(IElement element);

      bool CanEnter { get; }

      void Render(StringBuilder sb);

   }
}